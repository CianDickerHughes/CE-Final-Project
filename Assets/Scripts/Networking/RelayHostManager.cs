using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

/// <summary>
/// Manages relay hosting functionality for the CampaignManager scene.
/// Handles creating a relay server and displaying the join code.
/// </summary>
public class RelayHostManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button hostButton;
    [SerializeField] TextMeshProUGUI codeText;
    [SerializeField] Button copyButton;

    [Header("Network Prefabs")]
    [SerializeField] NetworkObject chatNetworkPrefab;
    [SerializeField] NetworkObject characterTransferPrefab;

    [Header("Settings")]
    [SerializeField] int maxConnections = 3;

    string currentJoinCode;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            // Only sign in if not already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"RelayHostManager: Signed in (PlayerId: {AuthenticationService.Instance.PlayerId})");
            }
            else
            {
                Debug.Log($"RelayHostManager: Already signed in (PlayerId: {AuthenticationService.Instance.PlayerId})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RelayHostManager: Failed to initialize/authenticate: {ex}");
            return;
        }

        // Setup host button listener
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(() =>
            {
                CreateRelay();
            });
        }
        else
        {
            Debug.LogWarning($"RelayHostManager: 'hostButton' is not assigned on '{gameObject.name}'. Host functionality will be disabled.");
        }

        if (copyButton != null)
        {
            copyButton.interactable = false;
            copyButton.onClick.AddListener(CopyJoinCodeToClipboard);
        }
    }

    /// <summary>
    /// Creates a relay allocation and generates a join code for clients.
    /// </summary>
    async void CreateRelay()
    {
        try
        {
            // Create a relay allocation with the specified max connections
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            
            // Get the join code for clients
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            currentJoinCode = joinCode;
            
            // Display the join code
            if (codeText != null)
            {
                codeText.text = joinCode;
            }

            if (copyButton != null)
            {
                copyButton.interactable = true;
            }
            else
            {
                Debug.Log($"RelayHostManager: Join Code: {joinCode} (no UI text assigned)");
            }
            
            Debug.Log($"RelayHostManager: Relay created successfully with join code: {joinCode}");
            
            // Check if NetworkManager exists
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("RelayHostManager: NetworkManager.Singleton is null! Add a NetworkManager GameObject to your scene.");
                if (codeText != null)
                {
                    codeText.text = "Error: No NetworkManager found";
                }
                return;
            }
            
            // Configure Unity Transport with relay server data
            try
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );
                Debug.Log("RelayHostManager: Relay server data configured");
            }
            catch (System.Exception relayEx)
            {
                Debug.LogWarning($"RelayHostManager: Could not configure relay data: {relayEx}. Continuing without relay.");
            }
            
            // Start as host
            NetworkManager.Singleton.StartHost();
            Debug.Log("RelayHostManager: Started as host");

            // Spawn networked chat so clients can message across scenes
            if (chatNetworkPrefab != null)
            {
                var spawned = Instantiate(chatNetworkPrefab);
                spawned.Spawn();
                Debug.Log("RelayHostManager: Spawned ChatNetwork prefab");
            }
            else
            {
                Debug.LogWarning("RelayHostManager: ChatNetwork prefab not assigned. Chat won't sync across network.");
            }

            // Spawn character transfer handler so clients can upload sheets
            if (characterTransferPrefab != null)
            {
                var transfer = Instantiate(characterTransferPrefab);
                transfer.Spawn();
                Debug.Log("RelayHostManager: Spawned CharacterTransferNetwork prefab");
            }
            else
            {
                Debug.LogWarning("RelayHostManager: CharacterTransferNetwork prefab not assigned. Character uploads won't reach host.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RelayHostManager: Failed to create relay: {ex}");
            
            if (codeText != null)
            {
                codeText.text = "Error: Failed to create relay";
            }
        }
    }

    void CopyJoinCodeToClipboard()
    {
        if (string.IsNullOrEmpty(currentJoinCode))
        {
            Debug.LogWarning("RelayHostManager: No join code to copy yet.");
            return;
        }

        GUIUtility.systemCopyBuffer = currentJoinCode;
        Debug.Log("RelayHostManager: Join code copied to clipboard");
    }
}

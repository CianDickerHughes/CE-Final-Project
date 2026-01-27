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
    [SerializeField] Button backButton;

    [Header("Network Prefabs")]
    [SerializeField] NetworkObject chatNetworkPrefab;
    [SerializeField] NetworkObject characterTransferPrefab;
    [SerializeField] NetworkObject sceneDataNetworkPrefab;
    [SerializeField] NetworkObject playerConnectionManagerPrefab;

    [Header("Settings")]
    [SerializeField] int maxConnections = 3;

    string currentJoinCode;

    void Awake()
    {
        // Try to restore join code immediately on Awake (before async Start)
        // This ensures UI is updated as soon as possible when returning to this scene
        RestoreJoinCodeIfActive();
    }

    void OnEnable()
    {
        // Also try in OnEnable in case serialized fields weren't ready in Awake
        RestoreJoinCodeIfActive();
    }

    void LateUpdate()
    {
        // One-time UI update check - ensures text gets set after all initialization
        if (!string.IsNullOrEmpty(currentJoinCode) && codeText != null && codeText.text != currentJoinCode)
        {
            codeText.text = currentJoinCode;
            Debug.Log($"RelayHostManager: LateUpdate set codeText to: {currentJoinCode}");
        }
    }

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

        // Check if we already have an active relay session (returning from another scene)
        RestoreJoinCodeIfActive();

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
            // Enable copy button if we already have a join code
            copyButton.interactable = !string.IsNullOrEmpty(currentJoinCode);
            copyButton.onClick.AddListener(CopyJoinCodeToClipboard);
        }

        // Setup back button listener to shutdown the relay/host
        if (backButton != null)
        {
            backButton.onClick.AddListener(ShutdownRelay);
        }
        else
        {
            Debug.LogWarning($"RelayHostManager: 'backButton' is not assigned on '{gameObject.name}'. Back functionality will be disabled.");
        }
    }

    /// <summary>
    /// Restores the join code from storage if we're returning to this scene with an active relay.
    /// </summary>
    void RestoreJoinCodeIfActive()
    {
        // First check if we have a stored join code
        string storedCode = RelayCodeStore.GetJoinCode();
        
        if (string.IsNullOrEmpty(storedCode))
        {
            Debug.Log("RelayHostManager: No stored join code found");
            return;
        }
        
        // Check if NetworkManager is running as host (meaning relay is still active)
        bool isHosting = NetworkManager.Singleton != null && 
                         NetworkManager.Singleton.IsHost && 
                         NetworkManager.Singleton.IsListening;
        
        Debug.Log($"RelayHostManager: Checking restore - StoredCode: {storedCode}, IsHosting: {isHosting}");
        
        if (isHosting || RelayCodeStore.HasActiveRelay())
        {
            currentJoinCode = storedCode;
            
            // Update UI
            if (codeText != null)
            {
                codeText.text = currentJoinCode;
                Debug.Log($"RelayHostManager: Set codeText to: {currentJoinCode}");
            }
            else
            {
                Debug.LogWarning("RelayHostManager: codeText is null, cannot display join code");
            }
            
            if (copyButton != null)
            {
                copyButton.interactable = true;
            }
            
            // Disable host button since we're already hosting
            if (hostButton != null)
            {
                hostButton.interactable = false;
            }
            
            Debug.Log($"RelayHostManager: Restored join code from storage: {currentJoinCode}");
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
            
            // Store the join code so it persists across scene changes
            RelayCodeStore.SetJoinCode(joinCode);
            
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
            
            // Disable host button since we're now hosting
            if (hostButton != null)
            {
                hostButton.interactable = false;
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

            // Spawn scene data network so DM can sync CurrentScene.json to clients
            if (sceneDataNetworkPrefab != null)
            {
                var sceneNetwork = Instantiate(sceneDataNetworkPrefab);
                sceneNetwork.Spawn();
                Debug.Log("RelayHostManager: Spawned SceneDataNetwork prefab");
            }
            else
            {
                Debug.LogWarning("RelayHostManager: SceneDataNetwork prefab not assigned. Scene data will not sync to clients.");
            }

            // Spawn player connection manager to track connected players and handle character assignments
            if (playerConnectionManagerPrefab != null)
            {
                var connectionManager = Instantiate(playerConnectionManagerPrefab);
                connectionManager.Spawn();
                Debug.Log("RelayHostManager: Spawned PlayerConnectionManager prefab");
            }
            else
            {
                Debug.LogWarning("RelayHostManager: PlayerConnectionManager prefab not assigned. Player tracking and character assignments won't work.");
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

    /// <summary>
    /// Shuts down Netcode/Relay when user presses back.
    /// </summary>
    public void ShutdownRelay()
    {
        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("RelayHostManager: Network shutdown");
            }

            currentJoinCode = string.Empty;
            
            // Clear the stored join code
            RelayCodeStore.Clear();

            if (codeText != null)
            {
                codeText.text = string.Empty;
            }
            if (copyButton != null)
            {
                copyButton.interactable = false;
            }
            if (hostButton != null)
            {
                hostButton.interactable = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"RelayHostManager: Exception while shutting down: {ex}");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

/// <summary>
/// Manages relay client functionality for the Campaigns scene.
/// Handles joining an existing relay using a join code.
/// </summary>
public class RelayClientManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] string waitingRoomSceneName = "WaitingRoom";

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            // Only sign in if not already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"RelayClientManager: Signed in (PlayerId: {AuthenticationService.Instance.PlayerId})");
            }
            else
            {
                Debug.Log($"RelayClientManager: Already signed in (PlayerId: {AuthenticationService.Instance.PlayerId})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RelayClientManager: Failed to initialize/authenticate: {ex}");
            
            if (statusText != null)
            {
                statusText.text = "Error: Failed to initialize services";
            }
            return;
        }

        // Setup join button listener
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(() =>
            {
                string code = (joinInput != null) ? joinInput.text : string.Empty;
                JoinRelay(code);
            });
        }
        else
        {
            Debug.LogWarning($"RelayClientManager: 'joinButton' is not assigned on '{gameObject.name}'. Join functionality will be disabled.");
        }
    }

    /// <summary>
    /// Joins a relay using the provided join code and transitions to the waiting room.
    /// </summary>
    /// <param name="joinCode">The join code provided by the host</param>
    async void JoinRelay(string joinCode)
    {
        try
        {
            // Validate join code
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogWarning("RelayClientManager: Join code is empty.");
                
                if (statusText != null)
                {
                    statusText.text = "Please enter a join code";
                }
                return;
            }

            // Update status
            if (statusText != null)
            {
                statusText.text = "Joining...";
            }

            // Join the relay
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            Debug.Log($"RelayClientManager: Successfully joined relay with code '{joinCode}'.");
            
            // Check if NetworkManager exists
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("RelayClientManager: NetworkManager.Singleton is null! Add a NetworkManager GameObject to your scene.");
                if (statusText != null)
                {
                    statusText.text = "Error: No NetworkManager found";
                }
                return;
            }
            
            // Configure Unity Transport with relay server data
            try
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

                Debug.Log("RelayClientManager: Relay server data configured");
            }
            catch (System.Exception relayEx)
            {
                Debug.LogWarning($"RelayClientManager: Could not configure relay data: {relayEx}. Continuing without relay.");
            }
            
            // Start as client
            NetworkManager.Singleton.StartClient();
            Debug.Log("RelayClientManager: Started as client");

            // Do not load local scene; host will drive synchronized scene
            if (statusText != null)
            {
                statusText.text = "Connected. Waiting for host...";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RelayClientManager: Failed to join relay with code '{joinCode}': {ex}");
            
            if (statusText != null)
            {
                statusText.text = "Error: Failed to join relay";
            }
        }
    }
}

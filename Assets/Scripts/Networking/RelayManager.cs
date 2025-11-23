using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;

public class RelayManager : MonoBehaviour
{

    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] TextMeshProUGUI codeText;

    async void Start() {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Try to auto-assign UI references if they weren't set in the Inspector
        if (hostButton == null) hostButton = GetComponent<Button>();
        if (joinButton == null) joinButton = GetComponent<Button>();
        if (joinInput == null) joinInput = GetComponent<TMP_InputField>();
        if (codeText == null) codeText = GetComponentInChildren<TextMeshProUGUI>();

        if (hostButton != null) {
            hostButton.onClick.AddListener( () => {
                CreateRelay();
            });
        } else {
            Debug.LogWarning($"RelayManager: 'hostButton' is not assigned on '{gameObject.name}'. Host functionality will be disabled.");
        }

        if (joinButton != null) {
            joinButton.onClick.AddListener( () => {
                string code = (joinInput != null) ? joinInput.text : string.Empty;
                JoinRelay(code);
            });
        } else {
            Debug.LogWarning($"RelayManager: 'joinButton' is not assigned on '{gameObject.name}'. Join functionality will be disabled.");
        }
    }

    async void CreateRelay(){
        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (codeText != null) {
                codeText.text = "Join Code: " + joinCode;
            } else {
                Debug.Log($"RelayManager: Join Code: {joinCode} (no UI text assigned)");
            }
        }
        catch (System.Exception ex) {
            Debug.LogError($"RelayManager: Failed to create relay: {ex}");
        }
    }

    async void JoinRelay(string joinCode){
        try {
            if (string.IsNullOrEmpty(joinCode)) {
                Debug.LogWarning("RelayManager: Join code is empty.");
                return;
            }
            await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (System.Exception ex) {
            Debug.LogError($"RelayManager: Failed to join relay with code '{joinCode}': {ex}");
        }
    }

}
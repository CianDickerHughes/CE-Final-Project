using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Client-side helper: when the player clicks Ready, send the selected character JSON + token image to the host via CharacterTransferNetwork.
/// Attach this in the WaitingRoom scene and wire the Ready button and optional status text.
/// </summary>
public class CharacterReadySender : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Scene Flow")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    [SerializeField] private bool loadGameplayAfterSend = true;

    [Header("Selection")]
    [Tooltip("If empty, uses CharacterSelectionContext.SelectedCharacterFilePath")] 
    [SerializeField] private string explicitCharacterJsonPath;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
        }
    }

    private void OnDestroy()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyClicked);
        }
    }

    private void OnReadyClicked()
    {
        var path = ResolveSelectedPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            SetStatus("Select a character first.");
            Debug.LogWarning("CharacterReadySender: No character JSON selected or file missing.");
            return;
        }

        if (CharacterTransferNetwork.Instance == null)
        {
            SetStatus("Character network not ready.");
            Debug.LogWarning("CharacterReadySender: CharacterTransferNetwork not found; host must spawn prefab.");
            return;
        }

        try
        {
            string jsonText = File.ReadAllText(path);
            var data = JsonUtility.FromJson<CharacterData>(jsonText);

            // Resolve token image bytes if present
            byte[] tokenBytes = Array.Empty<byte>();
            string tokenFileName = string.Empty;
            if (!string.IsNullOrEmpty(data?.tokenFileName))
            {
                string tokenPath = Path.Combine(Path.GetDirectoryName(path), data.tokenFileName);
                if (File.Exists(tokenPath))
                {
                    tokenBytes = File.ReadAllBytes(tokenPath);
                    tokenFileName = Path.GetFileName(tokenPath);
                }
                else
                {
                    Debug.LogWarning($"CharacterReadySender: Token file missing at {tokenPath}");
                }
            }

            string jsonFileName = Path.GetFileName(path);
            CharacterTransferNetwork.Instance.SendCharacterToHost(jsonFileName, jsonText, tokenFileName, tokenBytes);
            SetStatus("Sent to host.");

            if (loadGameplayAfterSend && !string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
        }
        catch (Exception ex)
        {
            SetStatus("Failed to send character.");
            Debug.LogError($"CharacterReadySender: Error sending character: {ex}");
        }
    }

    private string ResolveSelectedPath()
    {
        if (!string.IsNullOrEmpty(explicitCharacterJsonPath))
            return explicitCharacterJsonPath;

        return CharacterSelectionContext.SelectedCharacterFilePath;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
}

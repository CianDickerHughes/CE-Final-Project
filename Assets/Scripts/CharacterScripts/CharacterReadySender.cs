using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Client-side helper: when the player clicks Ready, send the selected character JSON + token image to the host via CharacterTransferNetwork.
/// Attach this in the WaitingRoom scene and wire the Ready button and optional status text.
/// Users can continue without selecting a character if allowContinueWithoutCharacter is true.
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
    
    [Tooltip("Allow players to continue to gameplay without selecting a character")]
    [SerializeField] private bool allowContinueWithoutCharacter = true;

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
        Debug.Log("CharacterReadySender: Ready button clicked!");
        
        var path = ResolveSelectedPath();
        Debug.Log($"CharacterReadySender: ResolveSelectedPath returned: '{path}'");
        
        // Check if we have a character selected
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            // No character selected - check if we allow continuing without one
            if (allowContinueWithoutCharacter)
            {
                SetStatus("Continuing without a character...");
                Debug.Log("CharacterReadySender: No character selected, continuing to gameplay.");
                
                if (loadGameplayAfterSend && !string.IsNullOrEmpty(gameplaySceneName))
                {
                    SceneManager.LoadScene(gameplaySceneName);
                }
                return;
            }
            else
            {
                SetStatus("Select a character first.");
                Debug.LogWarning("CharacterReadySender: No character JSON selected or file missing.");
                return;
            }
        }

        // Character is selected - send to host
        if (CharacterTransferNetwork.Instance == null)
        {
            // If network not available, just continue to gameplay
            Debug.LogWarning("CharacterReadySender: CharacterTransferNetwork.Instance is NULL! The prefab may not be spawned on the network. Continuing without sending to host.");
            SetStatus("Continuing to game...");
            
            if (loadGameplayAfterSend && !string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            return;
        }
        
        Debug.Log($"CharacterReadySender: CharacterTransferNetwork.Instance found, IsSpawned={CharacterTransferNetwork.Instance.IsSpawned}");

        try
        {
            Debug.Log($"CharacterReadySender: Reading character from path: {path}");
            string jsonText = File.ReadAllText(path);
            var data = JsonUtility.FromJson<CharacterData>(jsonText);
            Debug.Log($"CharacterReadySender: Loaded character '{data?.charName}', tokenFileName='{data?.tokenFileName}'");

            // Resolve token image bytes if present
            byte[] tokenBytes = Array.Empty<byte>();
            string tokenFileName = string.Empty;
            
            // Max size for network transfer (1MB - chunked transfer handles splitting)
            const int maxTokenSize = 1024 * 1024;
            
            // Size threshold above which we'll compress the image to reduce transfer time
            const int compressThreshold = 256 * 1024; // 256KB
            
            if (!string.IsNullOrEmpty(data?.tokenFileName))
            {
                string jsonFolder = Path.GetDirectoryName(path);
                string tokenPath = Path.Combine(jsonFolder, data.tokenFileName);
                Debug.Log($"CharacterReadySender: JSON folder: {jsonFolder}");
                Debug.Log($"CharacterReadySender: Looking for token at: {tokenPath}");
                
                if (File.Exists(tokenPath))
                {
                    byte[] originalBytes = File.ReadAllBytes(tokenPath);
                    Debug.Log($"CharacterReadySender: Token file found, size: {originalBytes.Length} bytes ({originalBytes.Length / 1024}KB)");
                    
                    if (originalBytes.Length <= compressThreshold)
                    {
                        // Small enough to send as-is
                        tokenBytes = originalBytes;
                        tokenFileName = Path.GetFileName(tokenPath);
                        Debug.Log($"CharacterReadySender: Token ready to send: {tokenFileName} ({tokenBytes.Length} bytes)");
                    }
                    else if (originalBytes.Length <= maxTokenSize)
                    {
                        // Large but under max - compress for faster transfer
                        Debug.Log($"CharacterReadySender: Token image is {originalBytes.Length / 1024}KB, compressing for faster transfer...");
                        
                        try
                        {
                            Texture2D tex = new Texture2D(2, 2);
                            tex.LoadImage(originalBytes);
                            
                            // Scale down large images
                            int maxDimension = 512;
                            if (tex.width > maxDimension || tex.height > maxDimension)
                            {
                                float scale = Mathf.Min((float)maxDimension / tex.width, (float)maxDimension / tex.height);
                                int newWidth = Mathf.RoundToInt(tex.width * scale);
                                int newHeight = Mathf.RoundToInt(tex.height * scale);
                                
                                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
                                Graphics.Blit(tex, rt);
                                
                                Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
                                RenderTexture.active = rt;
                                resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                                resized.Apply();
                                RenderTexture.active = null;
                                RenderTexture.ReleaseTemporary(rt);
                                
                                UnityEngine.Object.Destroy(tex);
                                tex = resized;
                            }
                            
                            // Encode as JPG for smaller size
                            tokenBytes = tex.EncodeToJPG(85);
                            tokenFileName = Path.GetFileNameWithoutExtension(data.tokenFileName) + "_compressed.jpg";
                            UnityEngine.Object.Destroy(tex);
                            
                            Debug.Log($"CharacterReadySender: Compressed token to {tokenBytes.Length / 1024}KB");
                        }
                        catch (Exception compressEx)
                        {
                            Debug.LogWarning($"CharacterReadySender: Failed to compress, sending original: {compressEx.Message}");
                            tokenBytes = originalBytes;
                            tokenFileName = Path.GetFileName(tokenPath);
                        }
                    }
                    else
                    {
                        // Image is too large even for chunked transfer
                        Debug.LogWarning($"CharacterReadySender: Token image is {originalBytes.Length / 1024}KB, exceeds maximum ({maxTokenSize / 1024}KB). Skipping.");
                    }
                }
                else
                {
                    Debug.LogWarning($"CharacterReadySender: Token file missing at {tokenPath}");
                }
            }
            else
            {
                Debug.Log($"CharacterReadySender: No tokenFileName in character data (tokenFileName is null or empty)");
            }

            string jsonFileName = Path.GetFileName(path);
            Debug.Log($"CharacterReadySender: Calling SendCharacterToHost with jsonFileName={jsonFileName}, tokenFileName={tokenFileName}, tokenBytes.Length={tokenBytes.Length}");
            CharacterTransferNetwork.Instance.SendCharacterToHost(jsonFileName, jsonText, tokenFileName, tokenBytes);
            SetStatus("Character sent to host!");

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

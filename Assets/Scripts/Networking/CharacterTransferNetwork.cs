using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Receives character sheets (JSON + optional token image) from clients and writes them to disk on the host.
/// Spawn this as a network prefab (host only) similar to ChatNetwork so clients can call into it.
/// Uses chunked transfer for reliable image transfer over the network.
/// </summary>
public class CharacterTransferNetwork : NetworkBehaviour
{
    public static CharacterTransferNetwork Instance { get; private set; }

    // Raised after the host stores a character. Useful for UI confirmations.
    public event Action<string, string> OnCharacterStored;

    // Chunk size for image transfer (8KB per chunk to stay well under RPC limits)
    private const int CHUNK_SIZE = 8 * 1024;

    // Server-side: pending image transfers indexed by client ID
    private Dictionary<ulong, PendingTransfer> pendingTransfers = new Dictionary<ulong, PendingTransfer>();

    private class PendingTransfer
    {
        public string JsonFileName;
        public string JsonContent;
        public string TokenFileName;
        public int TotalChunks;
        public string[] StringChunks;  // Base64 string chunks for reliable transfer
        public int ReceivedChunks;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Client entry point: send character JSON + optional token bytes to the host.
    /// Uses chunked transfer for images to avoid RPC size limits.
    /// </summary>
    public void SendCharacterToHost(string jsonFileName, string jsonContent, string tokenFileName, byte[] tokenBytes)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("CharacterTransferNetwork: Not spawned yet; host must spawn the prefab.");
            return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("CharacterTransferNetwork: Network not running; cannot send.");
            return;
        }

        // Normalize nulls
        var safeJsonName = jsonFileName ?? string.Empty;
        var safeJsonContent = jsonContent ?? string.Empty;
        var safeTokenName = tokenFileName ?? string.Empty;
        var safeTokenBytes = tokenBytes ?? Array.Empty<byte>();

        Debug.Log($"CharacterTransferNetwork: Sending character to host - JSON: {safeJsonName}, Token: {safeTokenName}, TokenBytes: {safeTokenBytes.Length} bytes");

        // Convert image to Base64 string for reliable network transfer
        string tokenBase64 = safeTokenBytes.Length > 0 ? Convert.ToBase64String(safeTokenBytes) : string.Empty;
        
        Debug.Log($"CharacterTransferNetwork: Converted to Base64, length: {tokenBase64.Length} chars");

        // Calculate number of chunks needed (4KB string chunks to stay safe with RPC limits)
        const int STRING_CHUNK_SIZE = 4 * 1024;
        int totalChunks = tokenBase64.Length > 0 
            ? (int)Math.Ceiling((double)tokenBase64.Length / STRING_CHUNK_SIZE) 
            : 0;

        Debug.Log($"CharacterTransferNetwork: Will send {totalChunks} chunks");

        // First, send the metadata (JSON content + info about incoming chunks)
        SendCharacterMetadataServerRpc(safeJsonName, safeJsonContent, safeTokenName, totalChunks);

        // Then send each chunk as a string
        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * STRING_CHUNK_SIZE;
            int length = Math.Min(STRING_CHUNK_SIZE, tokenBase64.Length - offset);
            string chunk = tokenBase64.Substring(offset, length);

            Debug.Log($"CharacterTransferNetwork: Sending chunk {i + 1}/{totalChunks} ({chunk.Length} chars)");
            SendTokenChunkServerRpc(i, chunk);
        }

        // If no chunks (no image), the metadata RPC will handle saving directly
        if (totalChunks == 0)
        {
            Debug.Log("CharacterTransferNetwork: No token image to send, metadata only.");
        }
    }

    /// <summary>
    /// Server receives character metadata and prepares for chunked image transfer.
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendCharacterMetadataServerRpc(string jsonFileName, string jsonContent, string tokenFileName, int totalChunks, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        Debug.Log($"CharacterTransferNetwork: SERVER received metadata from client {senderId} - JSON: {jsonFileName}, Token: {tokenFileName}, TotalChunks: {totalChunks}");

        if (totalChunks == 0)
        {
            // No image - save immediately
            SaveCharacter(senderId, jsonFileName, jsonContent, tokenFileName, Array.Empty<byte>());
        }
        else
        {
            // Prepare to receive chunks
            pendingTransfers[senderId] = new PendingTransfer
            {
                JsonFileName = jsonFileName,
                JsonContent = jsonContent,
                TokenFileName = tokenFileName,
                TotalChunks = totalChunks,
                StringChunks = new string[totalChunks],
                ReceivedChunks = 0
            };
            Debug.Log($"CharacterTransferNetwork: SERVER prepared to receive {totalChunks} chunks from client {senderId}");
        }
    }

    /// <summary>
    /// Server receives a chunk of the token image (as Base64 string).
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendTokenChunkServerRpc(int chunkIndex, string chunkData, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        Debug.Log($"CharacterTransferNetwork: SERVER received chunk {chunkIndex} from client {senderId} ({chunkData?.Length ?? 0} chars)");

        if (!pendingTransfers.TryGetValue(senderId, out var transfer))
        {
            Debug.LogWarning($"CharacterTransferNetwork: Received chunk from client {senderId} but no pending transfer exists.");
            return;
        }

        if (chunkIndex < 0 || chunkIndex >= transfer.TotalChunks)
        {
            Debug.LogWarning($"CharacterTransferNetwork: Invalid chunk index {chunkIndex} from client {senderId}.");
            return;
        }

        // Store the chunk
        if (transfer.StringChunks[chunkIndex] == null)
        {
            transfer.StringChunks[chunkIndex] = chunkData;
            transfer.ReceivedChunks++;
            Debug.Log($"CharacterTransferNetwork: SERVER stored chunk {chunkIndex + 1}/{transfer.TotalChunks}, total received: {transfer.ReceivedChunks}");
        }

        // Check if all chunks received
        if (transfer.ReceivedChunks >= transfer.TotalChunks)
        {
            Debug.Log($"CharacterTransferNetwork: All {transfer.TotalChunks} chunks received from client {senderId}. Reassembling...");

            // Reassemble the Base64 string
            var sb = new System.Text.StringBuilder();
            foreach (var chunk in transfer.StringChunks)
            {
                if (chunk != null)
                {
                    sb.Append(chunk);
                }
            }

            string fullBase64 = sb.ToString();
            Debug.Log($"CharacterTransferNetwork: Reassembled Base64 string, length: {fullBase64.Length} chars");

            // Convert back to bytes
            byte[] fullImage = Array.Empty<byte>();
            try
            {
                fullImage = Convert.FromBase64String(fullBase64);
                Debug.Log($"CharacterTransferNetwork: Decoded to {fullImage.Length} bytes");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CharacterTransferNetwork: Failed to decode Base64: {ex.Message}");
            }

            // Save the character
            SaveCharacter(senderId, transfer.JsonFileName, transfer.JsonContent, transfer.TokenFileName, fullImage);

            // Clean up
            pendingTransfers.Remove(senderId);
        }
    }

    /// <summary>
    /// Saves the character files to disk.
    /// </summary>
    private void SaveCharacter(ulong senderId, string jsonFileName, string jsonContent, string tokenFileName, byte[] tokenBytes)
    {
        try
        {
            // Get the PlayerCharacters folder for the current campaign
            string folder = GetPlayerCharactersFolder();

            if (string.IsNullOrEmpty(folder))
            {
                Debug.LogError($"CharacterTransferNetwork: Could not determine PlayerCharacters folder for current campaign.");
                return;
            }

            // Sanitize names and ensure extensions
            string jsonName = BuildJsonFileName(jsonFileName);
            string tokenName = BuildTokenFileName(tokenFileName);

            // Update the JSON content to use the correct token filename that will be saved on this host
            string updatedJsonContent = jsonContent ?? string.Empty;
            if (!string.IsNullOrEmpty(tokenName) && tokenBytes != null && tokenBytes.Length > 0)
            {
                try
                {
                    // Parse and update the tokenFileName in the JSON
                    var charData = JsonUtility.FromJson<CharacterData>(updatedJsonContent);
                    if (charData != null)
                    {
                        Debug.Log($"CharacterTransferNetwork: Updating tokenFileName from '{charData.tokenFileName}' to '{tokenName}'");
                        charData.tokenFileName = tokenName;
                        updatedJsonContent = JsonUtility.ToJson(charData, true);
                    }
                }
                catch (Exception parseEx)
                {
                    Debug.LogWarning($"CharacterTransferNetwork: Could not update tokenFileName in JSON: {parseEx.Message}");
                }
            }

            // Write JSON
            string jsonPath = Path.Combine(folder, jsonName);
            File.WriteAllText(jsonPath, updatedJsonContent);

            // Write token if provided
            string tokenPath = null;
            if (tokenBytes != null && tokenBytes.Length > 0 && !string.IsNullOrEmpty(tokenName))
            {
                tokenPath = Path.Combine(folder, tokenName);
                File.WriteAllBytes(tokenPath, tokenBytes);
                Debug.Log($"CharacterTransferNetwork: Saved token image ({tokenBytes.Length} bytes) to {tokenPath}");
            }

            Debug.Log($"CharacterTransferNetwork: Stored character from client {senderId} -> {jsonPath} (token: {tokenPath ?? "<none>"})");

            CharacterStoredClientRpc(jsonName, tokenName);
            OnCharacterStored?.Invoke(jsonName, tokenName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CharacterTransferNetwork: Failed to store character from client {senderId}: {ex}");
        }
    }
    
    /// <summary>
    /// Gets the PlayerCharacters folder for the current campaign.
    /// Creates it if it doesn't exist.
    /// </summary>
    private string GetPlayerCharactersFolder()
    {
        // Try multiple sources to get the campaign info
        string campaignId = null;
        string campaignFilePath = null;
        
        // 1. Try SceneDataTransfer first
        if (SceneDataTransfer.Instance != null)
        {
            campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            Debug.Log($"CharacterTransferNetwork: Got campaign ID from SceneDataTransfer: {campaignId ?? "null"}");
        }
        
        // 2. Fall back to CampaignSelectionContext (static, persists across scenes)
        if (string.IsNullOrEmpty(campaignId) && CampaignSelectionContext.HasSelection)
        {
            campaignId = CampaignSelectionContext.SelectedCampaignId;
            campaignFilePath = CampaignSelectionContext.SelectedCampaignFilePath;
            Debug.Log($"CharacterTransferNetwork: Got campaign ID from CampaignSelectionContext: {campaignId}");
        }
        
        if (string.IsNullOrEmpty(campaignId))
        {
            Debug.LogWarning("CharacterTransferNetwork: No campaign ID found, falling back to global Characters folder.");
            return CharacterIO.GetCharactersFolder();
        }
        
        // If we have the campaign file path, get the folder directly from it
        string campaignFolder = null;
        if (!string.IsNullOrEmpty(campaignFilePath) && File.Exists(campaignFilePath))
        {
            campaignFolder = Path.GetDirectoryName(campaignFilePath);
            Debug.Log($"CharacterTransferNetwork: Got campaign folder from file path: {campaignFolder}");
        }
        else
        {
            // Find the campaign folder by searching for the campaign JSON
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            Debug.Log($"CharacterTransferNetwork: Searching for campaign in: {campaignsFolder}");
            
            if (Directory.Exists(campaignsFolder))
            {
                foreach (string folder in Directory.GetDirectories(campaignsFolder))
                {
                    string campaignJsonPath = Path.Combine(folder, $"{campaignId}.json");
                    if (File.Exists(campaignJsonPath))
                    {
                        campaignFolder = folder;
                        Debug.Log($"CharacterTransferNetwork: Found campaign folder: {campaignFolder}");
                        break;
                    }
                }
            }
        }
        
        if (string.IsNullOrEmpty(campaignFolder))
        {
            Debug.LogWarning($"CharacterTransferNetwork: Campaign folder not found for ID {campaignId}, falling back to global Characters folder.");
            return CharacterIO.GetCharactersFolder();
        }
        
        // Create PlayerCharacters subfolder if it doesn't exist
        string playerCharactersFolder = Path.Combine(campaignFolder, "PlayerCharacters");
        if (!Directory.Exists(playerCharactersFolder))
        {
            Directory.CreateDirectory(playerCharactersFolder);
            Debug.Log($"CharacterTransferNetwork: Created PlayerCharacters folder at {playerCharactersFolder}");
        }
        
        return playerCharactersFolder;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CharacterStoredClientRpc(string jsonFileName, string tokenFileName)
    {
        OnCharacterStored?.Invoke(jsonFileName, tokenFileName);
    }

    private string BuildJsonFileName(string name)
    {
        // Drop any path and enforce .json extension
        string baseName = Path.GetFileNameWithoutExtension(string.IsNullOrWhiteSpace(name) ? "character" : name);
        baseName = CharacterIO.SanitizeFileName(baseName);
        return baseName + ".json";
    }

    private string BuildTokenFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        string baseName = Path.GetFileNameWithoutExtension(name);
        string ext = Path.GetExtension(name);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

        baseName = CharacterIO.SanitizeFileName(baseName);
        return baseName + ext;
    }
}

using System;
using System.IO;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Receives character sheets (JSON + optional token image) from clients and writes them to disk on the host.
/// Spawn this as a network prefab (host only) similar to ChatNetwork so clients can call into it.
/// </summary>
public class CharacterTransferNetwork : NetworkBehaviour
{
    public static CharacterTransferNetwork Instance { get; private set; }

    // Raised after the host stores a character. Useful for UI confirmations.
    public event Action<string, string> OnCharacterStored;

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

        // Normalize nulls to empty strings/arrays to keep RPC signature simple
        var safeJsonName = jsonFileName ?? string.Empty;
        var safeJsonContent = jsonContent ?? string.Empty;
        var safeTokenName = tokenFileName ?? string.Empty;
        var safeTokenBytes = tokenBytes ?? Array.Empty<byte>();

        SendCharacterServerRpc(safeJsonName, safeJsonContent, safeTokenName, safeTokenBytes);
    }

    /// <summary>
    /// Runs on host: saves files to Assets/Characters (Editor) or persistent data (builds), then notifies clients.
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendCharacterServerRpc(string jsonFileName, string jsonContent, string tokenFileName, byte[] tokenBytes, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        try
        {
            string folder = CharacterIO.GetCharactersFolder();

            // Sanitize names and ensure extensions
            string jsonName = BuildJsonFileName(jsonFileName);
            string tokenName = BuildTokenFileName(tokenFileName);

            // Write JSON
            string jsonPath = Path.Combine(folder, jsonName);
            File.WriteAllText(jsonPath, jsonContent ?? string.Empty);

            // Write token if provided
            string tokenPath = null;
            if (tokenBytes != null && tokenBytes.Length > 0 && !string.IsNullOrEmpty(tokenName))
            {
                tokenPath = Path.Combine(folder, tokenName);
                File.WriteAllBytes(tokenPath, tokenBytes);
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

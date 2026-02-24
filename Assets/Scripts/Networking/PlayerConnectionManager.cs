using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Services.Authentication;

/// <summary>
/// Manages connected players in the network session and handles character assignments.
/// Spawned by the host when hosting begins.
/// Tracks who is connected and allows the DM to assign PCs to characters.
/// </summary>
public class PlayerConnectionManager : NetworkBehaviour
{
    public static PlayerConnectionManager Instance { get; private set; }
    
    // Events for UI updates
    public event Action<ConnectedPlayer> OnPlayerConnected;
    public event Action<ulong> OnPlayerDisconnected;
    public event Action<string, string, string> OnCharacterAssigned; // characterId, playerId, username
    public event Action<string> OnCharacterUnassigned; // characterId
    public event Action OnPlayersListUpdated;
    
    // Server-side list of connected players
    private Dictionary<ulong, ConnectedPlayer> connectedPlayers = new Dictionary<ulong, ConnectedPlayer>();
    
    // NetworkList for syncing player data to all clients
    private NetworkList<PlayerNetworkData> syncedPlayers;
    
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
            return;
        }
        
        // Initialize the NetworkList
        syncedPlayers = new NetworkList<PlayerNetworkData>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Subscribe to connection events
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            
            // Add host as first player
            AddHostAsPlayer();
        }
        
        // All clients listen for sync list changes
        syncedPlayers.OnListChanged += OnSyncedPlayersChanged;
        
        Debug.Log($"PlayerConnectionManager spawned. IsServer: {IsServer}, IsClient: {IsClient}");
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
        
        syncedPlayers.OnListChanged -= OnSyncedPlayersChanged;
        
        base.OnNetworkDespawn();
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void AddHostAsPlayer()
    {
        if (!IsServer) return;
        
        string username = SessionManager.Instance?.CurrentUsername ?? "Host";
        string playerId = GetLocalPlayerId();
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        
        var hostPlayer = new ConnectedPlayer(clientId, username, playerId, true);
        connectedPlayers[clientId] = hostPlayer;
        
        // Add to synced list
        syncedPlayers.Add(new PlayerNetworkData
        {
            ClientId = clientId,
            Username = username,
            PlayerId = playerId,
            IsHost = true
        });
        
        Debug.Log($"Host added as player: {username} (ClientId: {clientId})");
        OnPlayerConnected?.Invoke(hostPlayer);
    }
    
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        
        // Skip host (already added)
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        
        Debug.Log($"Client connected: {clientId}. Requesting their info...");
        
        // Request player info from the client - send to all clients, they filter by clientId
        RequestPlayerInfoClientRpc(clientId);
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;
        
        if (connectedPlayers.TryGetValue(clientId, out var player))
        {
            connectedPlayers.Remove(clientId);
            
            // Remove from synced list
            for (int i = 0; i < syncedPlayers.Count; i++)
            {
                if (syncedPlayers[i].ClientId == clientId)
                {
                    syncedPlayers.RemoveAt(i);
                    break;
                }
            }
            
            Debug.Log($"Player disconnected: {player.username} (ClientId: {clientId})");
            OnPlayerDisconnected?.Invoke(clientId);
        }
    }
    
    /// <summary>
    /// Request player info from a specific client.
    /// Sent to all clients but only the targeted one responds.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void RequestPlayerInfoClientRpc(ulong targetClientId)
    {
        // Only the targeted client responds
        if (NetworkManager.Singleton.LocalClientId != targetClientId) 
        {
            Debug.Log($"RequestPlayerInfo: I am {NetworkManager.Singleton.LocalClientId}, ignoring request for {targetClientId}");
            return;
        }
        
        string username = SessionManager.Instance?.CurrentUsername ?? $"Player_{targetClientId}";
        string playerId = GetLocalPlayerId();
        
        Debug.Log($"RequestPlayerInfo: Sending my info to host: {username}, PlayerId: {playerId}");
        SendPlayerInfoServerRpc(username, playerId);
    }
    
    /// <summary>
    /// Client sends their player info to the server
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendPlayerInfoServerRpc(string username, string playerId, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        Debug.Log($"Server received player info: {username} (ClientId: {clientId}, PlayerId: {playerId})");
        
        var player = new ConnectedPlayer(clientId, username, playerId, false);
        connectedPlayers[clientId] = player;
        
        // Add to synced list
        syncedPlayers.Add(new PlayerNetworkData
        {
            ClientId = clientId,
            Username = username,
            PlayerId = playerId,
            IsHost = false
        });
        
        Debug.Log($"Player registered: {username} (ClientId: {clientId}, PlayerId: {playerId})");
        OnPlayerConnected?.Invoke(player);
        
        // Check for auto-assignment based on previous session
        CheckAutoAssignment(player);
    }
    
    /// <summary>
    /// Called when the synced player list changes (for UI updates on clients)
    /// </summary>
    private void OnSyncedPlayersChanged(NetworkListEvent<PlayerNetworkData> changeEvent)
    {
        Debug.Log($"Player list updated. Total players: {syncedPlayers.Count}");
        OnPlayersListUpdated?.Invoke();
    }
    
    /// <summary>
    /// Get the local Unity Authentication PlayerId
    /// </summary>
    private string GetLocalPlayerId()
    {
        try
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                return AuthenticationService.Instance.PlayerId;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not get PlayerId: {ex.Message}");
        }
        
        // Fallback to a generated ID if not authenticated
        return $"local_{SystemInfo.deviceUniqueIdentifier.Substring(0, 8)}";
    }
    
    /// <summary>
    /// Check if this player should be auto-assigned to a character based on previous sessions
    /// </summary>
    private void CheckAutoAssignment(ConnectedPlayer player)
    {
        if (!IsServer) return;
        
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        if (campaign?.characterAssignments == null) return;
        
        var assignment = campaign.characterAssignments.GetAssignmentForPlayer(player.playerId);
        if (assignment != null)
        {
            Debug.Log($"Auto-assigning {player.username} to character {assignment.characterId} (from previous session)");
            // The assignment already exists in campaign data, just notify
            OnCharacterAssigned?.Invoke(assignment.characterId, player.playerId, player.username);
        }
    }
    
    // ========== PUBLIC API FOR DM ==========
    
    /// <summary>
    /// Get all currently connected players (non-host only for assignment UI)
    /// </summary>
    public List<ConnectedPlayer> GetConnectedPlayers(bool includeHost = false)
    {
        var result = new List<ConnectedPlayer>();
        foreach (var player in connectedPlayers.Values)
        {
            if (includeHost || !player.isHost)
            {
                result.Add(player);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get synced player data (available on all clients)
    /// </summary>
    public List<PlayerNetworkData> GetSyncedPlayers(bool includeHost = false)
    {
        var result = new List<PlayerNetworkData>();
        foreach (var player in syncedPlayers)
        {
            if (includeHost || !player.IsHost)
            {
                result.Add(player);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get a connected player by their client ID
    /// </summary>
    public ConnectedPlayer GetPlayerByClientId(ulong clientId)
    {
        connectedPlayers.TryGetValue(clientId, out var player);
        return player;
    }
    
    /// <summary>
    /// Get a connected player by their PlayerId
    /// </summary>
    public ConnectedPlayer GetPlayerByPlayerId(string playerId)
    {
        foreach (var player in connectedPlayers.Values)
        {
            if (player.playerId == playerId)
                return player;
        }
        return null;
    }
    
    /// <summary>
    /// DM assigns a connected player to a character
    /// </summary>
    public void AssignPlayerToCharacter(string characterId, string playerId, string username)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can assign characters");
            return;
        }
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        if (campaign == null) return;
        
        if (campaign.characterAssignments == null)
        {
            campaign.characterAssignments = new CampaignCharacterAssignments();
        }
        
        campaign.characterAssignments.AssignPlayerToCharacter(characterId, playerId, username);
        CampaignManager.Instance.SaveCampaign();
        
        string characterDataJson = LoadCharacterDataAsJson(characterId);
        string imageBase64 = LoadCharacterImageAsBase64(characterId);
        
        if (string.IsNullOrEmpty(imageBase64))
        {
            Debug.LogWarning($"AssignPlayerToCharacter: No image found for character {characterId}. Character JSON will be sent but image will not be transmitted.");
        }
        
        string tokenFileName = null;
        if (!string.IsNullOrEmpty(characterDataJson))
        {
            try
            {
                var charData = JsonUtility.FromJson<CharacterData>(characterDataJson);
                if (charData != null) tokenFileName = charData.tokenFileName;
            }
            catch { }
        }
        
        OnCharacterAssigned?.Invoke(characterId, playerId, username);
        NotifyAssignmentClientRpc(characterId, playerId, username, characterDataJson ?? "", campaign.campaignName);
        
        if (!string.IsNullOrEmpty(imageBase64) && !string.IsNullOrEmpty(tokenFileName))
        {
            SendCharacterImageChunked(characterId, imageBase64, tokenFileName ?? "");
        }
    }
    
    /// <summary>
    /// Load character image from file system, compress to JPG, and encode as base64
    /// </summary>
    private string LoadCharacterImageAsBase64(string characterId)
    {
        try
        {
            var campaign = CampaignManager.Instance?.GetCurrentCampaign();
            if (campaign == null)
            {
                Debug.LogWarning("LoadCharacterImageAsBase64: Campaign is null");
                return null;
            }
            
            string characterDataJson = LoadCharacterDataAsJson(characterId);
            if (string.IsNullOrEmpty(characterDataJson))
            {
                Debug.LogWarning("LoadCharacterImageAsBase64: Character JSON not found");
                return null;
            }
            
            var characterData = JsonUtility.FromJson<CharacterData>(characterDataJson);
            if (characterData == null || string.IsNullOrEmpty(characterData.tokenFileName))
            {
                Debug.LogWarning("LoadCharacterImageAsBase64: Character data or tokenFileName is missing");
                return null;
            }
            
            string tokenFileName = characterData.tokenFileName;
            string playerCharactersFolder = GetPlayerCharactersFolderPath(campaign);
            Debug.Log($"LoadCharacterImageAsBase64: Looking for {tokenFileName} in {playerCharactersFolder}");
            
            byte[] imageBytes = null;
            string imageFilePath = Path.Combine(playerCharactersFolder, tokenFileName);
            
            Debug.Log($"LoadCharacterImageAsBase64: Full image path: {imageFilePath}");
            Debug.Log($"LoadCharacterImageAsBase64: File exists: {File.Exists(imageFilePath)}");
            
            if (File.Exists(imageFilePath))
            {
                Debug.Log($"LoadCharacterImageAsBase64: Found image file!");
                imageBytes = File.ReadAllBytes(imageFilePath);
            }
            else
            {
                Debug.LogWarning($"LoadCharacterImageAsBase64: Image file not found at {imageFilePath}");
                return null;
            }
            
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogWarning("LoadCharacterImageAsBase64: Image bytes are empty");
                return null;
            }
            
            Debug.Log($"LoadCharacterImageAsBase64: Loaded {imageBytes.Length} bytes, compressing to JPG");
            
            // Load as Texture2D and compress to JPG for smaller network transmission
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError("LoadCharacterImageAsBase64: Failed to load image into Texture2D");
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            
            byte[] compressedBytes = ImageConversion.EncodeToJPG(texture, 90);
            UnityEngine.Object.Destroy(texture);
            
            if (compressedBytes == null || compressedBytes.Length == 0)
            {
                Debug.LogError("LoadCharacterImageAsBase64: Compression produced empty result");
                return null;
            }
            
            string result = System.Convert.ToBase64String(compressedBytes);
            Debug.Log($"LoadCharacterImageAsBase64: Successfully compressed and encoded to base64");
            return result;
        }
        catch (Exception ex) 
        {
            Debug.LogError($"LoadCharacterImageAsBase64 exception: {ex}");
            return null;
        }
    }
    
    
    /// <summary>
    /// Load character data from file and return as JSON string
    /// Searches for the character by ID in the PlayerCharacters folder
    /// </summary>
    private string LoadCharacterDataAsJson(string characterId)
    {
        try
        {
            var campaign = CampaignManager.Instance?.GetCurrentCampaign();
            if (campaign == null) return null;
            
            string playerCharactersFolder = GetPlayerCharactersFolderPath(campaign);
            if (string.IsNullOrEmpty(playerCharactersFolder) || !Directory.Exists(playerCharactersFolder)) return null;
            
            string[] files = Directory.GetFiles(playerCharactersFolder, "*.json");
            
            foreach (string filePath in files)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var charData = JsonUtility.FromJson<CharacterData>(json);
                    if (charData != null && charData.id == characterId)
                    {
                        return json;
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadCharacterDataAsJson failed: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Get the PlayerCharacters folder for the campaign
    /// Uses the same logic as DMCharacterAssignmentUI to find the correct path
    /// </summary>
    private string GetPlayerCharactersFolderPath(Campaign campaign)
    {
        if (campaign == null) return null;
        
        // Direct approach: use campaignName to find the folder in Campaigns directory
        string campaignsFolder = CampaignManager.GetCampaignsFolder();
        if (!string.IsNullOrEmpty(campaignsFolder) && Directory.Exists(campaignsFolder))
        {
            string campaignFolder = Path.Combine(campaignsFolder, campaign.campaignName);
            string playerCharactersPath = Path.Combine(campaignFolder, "PlayerCharacters");
            Debug.Log($"GetPlayerCharactersFolderPath: Checking {playerCharactersPath}");
            if (Directory.Exists(playerCharactersPath))
            {
                Debug.Log($"GetPlayerCharactersFolderPath: Found folder!");
                return playerCharactersPath;
            }
            else
            {
                Debug.LogWarning($"GetPlayerCharactersFolderPath: Folder does not exist at {playerCharactersPath}");
            }
        }
        
        // Fallback: CampaignSelectionContext
        if (CampaignSelectionContext.HasSelection && !string.IsNullOrEmpty(CampaignSelectionContext.SelectedCampaignFilePath))
        {
            string playerCharactersPath = Path.Combine(Path.GetDirectoryName(CampaignSelectionContext.SelectedCampaignFilePath), "PlayerCharacters");
            if (Directory.Exists(playerCharactersPath)) return playerCharactersPath;
        }
        
        // Fallback: SceneDataTransfer
        if (SceneDataTransfer.Instance != null)
        {
            string campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            if (!string.IsNullOrEmpty(campaignId))
            {
                string campaignsFolder2 = CampaignManager.GetCampaignsFolder();
                if (!string.IsNullOrEmpty(campaignsFolder2) && Directory.Exists(campaignsFolder2))
                {
                    foreach (string folder in Directory.GetDirectories(campaignsFolder2))
                    {
                        if (File.Exists(Path.Combine(folder, $"{campaignId}.json")))
                        {
                            string playerCharactersPath = Path.Combine(folder, "PlayerCharacters");
                            return playerCharactersPath;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// DM removes assignment from a character
    /// </summary>
    public void UnassignCharacter(string characterId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can unassign characters");
            return;
        }
        
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        if (campaign?.characterAssignments == null) return;
        
        campaign.characterAssignments.UnassignCharacter(characterId);
        CampaignManager.Instance.SaveCampaign();
        
        Debug.Log($"Unassigned character {characterId}");
        
        OnCharacterUnassigned?.Invoke(characterId);
        NotifyUnassignmentClientRpc(characterId);
    }
    
    /// <summary>
    /// Check if a character is assigned to any player
    /// </summary>
    public bool IsCharacterAssigned(string characterId)
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.IsCharacterAssigned(characterId) ?? false;
    }
    
    /// <summary>
    /// Get the assignment info for a character
    /// </summary>
    public CharacterPlayerAssignment GetCharacterAssignment(string characterId)
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.GetAssignmentForCharacter(characterId);
    }
    
    /// <summary>
    /// Get all current assignments
    /// </summary>
    public List<CharacterPlayerAssignment> GetAllAssignments()
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.assignments ?? new List<CharacterPlayerAssignment>();
    }
    
    // ========== NETWORK SYNC RPCs ==========
    
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyAssignmentClientRpc(string characterId, string playerId, string username, string characterDataJson, string campaignName)
    {
        if (!string.IsNullOrEmpty(characterDataJson))
        {
            PlayerAssignmentHelper.Instance?.CacheCharacterData(characterId, characterDataJson, campaignName);
        }
        
        OnCharacterAssigned?.Invoke(characterId, playerId, username);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyUnassignmentClientRpc(string characterId)
    {
        Debug.Log($"Character unassigned: {characterId}");
        OnCharacterUnassigned?.Invoke(characterId);
    }
    
    // ========== CHUNKED IMAGE TRANSFER ==========
    
    private const int IMAGE_CHUNK_SIZE = 4 * 1024; // 4KB chunks to stay under RPC limits
    
    /// <summary>
    /// Send character image in chunks to avoid RPC buffer overflow
    /// </summary>
    private void SendCharacterImageChunked(string characterId, string imageBase64, string tokenFileName)
    {
        int totalChunks = (int)System.Math.Ceiling((double)imageBase64.Length / IMAGE_CHUNK_SIZE);
        
        Debug.Log($"SendCharacterImageChunked: Sending {imageBase64.Length} bytes in {totalChunks} chunks for {characterId}");
        
        // Send metadata first (tells client how many chunks to expect)
        SendImageMetadataClientRpc(characterId, tokenFileName, totalChunks);
        
        // Send each chunk
        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * IMAGE_CHUNK_SIZE;
            int length = System.Math.Min(IMAGE_CHUNK_SIZE, imageBase64.Length - offset);
            string chunk = imageBase64.Substring(offset, length);
            
            SendImageChunkClientRpc(characterId, i, chunk);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SendImageMetadataClientRpc(string characterId, string tokenFileName, int totalChunks)
    {
        PlayerAssignmentHelper.Instance?.PrepareImageReception(characterId, tokenFileName, totalChunks);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SendImageChunkClientRpc(string characterId, int chunkIndex, string chunkData)
    {
        PlayerAssignmentHelper.Instance?.ReceiveImageChunk(characterId, chunkIndex, chunkData);
    }
}


/// <summary>
/// Serializable struct for network syncing player data.
/// Must be INetworkSerializable for NetworkList.
/// </summary>
public struct PlayerNetworkData : INetworkSerializable, IEquatable<PlayerNetworkData>
{
    public ulong ClientId;
    public FixedString64Bytes Username;
    public FixedString64Bytes PlayerId;
    public bool IsHost;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Username);
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref IsHost);
    }
    
    public bool Equals(PlayerNetworkData other)
    {
        return ClientId == other.ClientId;
    }
    
    public override bool Equals(object obj)
    {
        return obj is PlayerNetworkData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return ClientId.GetHashCode();
    }
}

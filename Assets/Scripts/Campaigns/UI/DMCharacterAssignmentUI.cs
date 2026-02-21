using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// UI Controller for the DM to assign connected players (PCs) to characters.
/// Displays a list of characters in the campaign and allows assigning players to them.
/// Place this on a panel/page in the CampaignManager or GameplayScene.
/// </summary>
public class DMCharacterAssignmentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform characterListContainer;  // Container for character assignment items
    [SerializeField] private GameObject assignmentItemPrefab;   // Prefab for each character row
    [SerializeField] private TextMeshProUGUI titleText;         // Optional title
    [SerializeField] private TextMeshProUGUI emptyText;         // Shown when no characters
    [SerializeField] private Button refreshButton;              // Manual refresh button
    
    [Header("Connected Players Panel")]
    [SerializeField] private Transform connectedPlayersContainer; // Shows who is connected
    [SerializeField] private GameObject connectedPlayerItemPrefab; // Prefab for each player row
    [SerializeField] private TextMeshProUGUI connectedPlayersTitle;
    
    [Header("Settings")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    [SerializeField] private bool showHostInPlayerList = false;
    
    // Cached data
    private List<CharacterData> campaignCharacters = new List<CharacterData>();
    private Dictionary<string, CharacterAssignmentItemUI> characterUIItems = new Dictionary<string, CharacterAssignmentItemUI>();
    
    void OnEnable()
    {
        // Subscribe to events
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnPlayerConnected += OnPlayerConnected;
            PlayerConnectionManager.Instance.OnPlayerDisconnected += OnPlayerDisconnected;
            PlayerConnectionManager.Instance.OnCharacterAssigned += OnCharacterAssigned;
            PlayerConnectionManager.Instance.OnCharacterUnassigned += OnCharacterUnassigned;
            PlayerConnectionManager.Instance.OnPlayersListUpdated += RefreshConnectedPlayersList;
        }
        
        if (autoRefreshOnEnable)
        {
            RefreshAll();
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnPlayerConnected -= OnPlayerConnected;
            PlayerConnectionManager.Instance.OnPlayerDisconnected -= OnPlayerDisconnected;
            PlayerConnectionManager.Instance.OnCharacterAssigned -= OnCharacterAssigned;
            PlayerConnectionManager.Instance.OnCharacterUnassigned -= OnCharacterUnassigned;
            PlayerConnectionManager.Instance.OnPlayersListUpdated -= RefreshConnectedPlayersList;
        }
    }
    
    void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshAll);
        }
    }
    
    /// <summary>
    /// Refresh both the character list and connected players list
    /// </summary>
    public void RefreshAll()
    {
        LoadCampaignCharacters();
        RefreshConnectedPlayersList();
    }
    
    /// <summary>
    /// Load all characters from the campaign's PlayerCharacters folder
    /// </summary>
    private void LoadCampaignCharacters()
    {
        campaignCharacters.Clear();
        characterUIItems.Clear();
        
        // Clear existing UI items
        if (characterListContainer != null)
        {
            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        string playerCharactersFolder = GetPlayerCharactersFolder();
        
        if (string.IsNullOrEmpty(playerCharactersFolder) || !Directory.Exists(playerCharactersFolder))
        {
            ShowEmptyState("No characters found.\nWait for players to join and submit their characters.");
            return;
        }
        
        string[] files = Directory.GetFiles(playerCharactersFolder, "*.json");
        
        if (files == null || files.Length == 0)
        {
            ShowEmptyState("No characters found.\nWait for players to join and submit their characters.");
            return;
        }
        
        // Hide empty text
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }
        
        // Load and create UI for each character
        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var characterData = JsonUtility.FromJson<CharacterData>(json);
                
                if (characterData == null)
                {
                    Debug.LogWarning($"DMCharacterAssignmentUI: Failed to deserialize {Path.GetFileName(filePath)}");
                    continue;
                }
                
                Debug.Log($"Loaded character: {characterData.charName} (id: {characterData.id})");
                campaignCharacters.Add(characterData);
                CreateCharacterAssignmentItem(characterData);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DMCharacterAssignmentUI: Failed to load character from {filePath}: {ex.Message}");
            }
        }
        
        // Update title
        if (titleText != null)
        {
            titleText.text = $"Character Assignments ({campaignCharacters.Count})";
        }
        
        Debug.Log($"DMCharacterAssignmentUI: Loaded {campaignCharacters.Count} characters");
    }
    
    /// <summary>
    /// Create a UI item for a character with assignment dropdown
    /// </summary>
    private void CreateCharacterAssignmentItem(CharacterData character)
    {
        if (assignmentItemPrefab == null || characterListContainer == null) return;
        
        GameObject itemGO = Instantiate(assignmentItemPrefab, characterListContainer);
        var itemUI = itemGO.GetComponent<CharacterAssignmentItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(character, GetConnectedPlayersForDropdown(), OnAssignmentChanged);
            characterUIItems[character.id] = itemUI;
            
            // Check if already assigned
            var assignment = PlayerConnectionManager.Instance?.GetCharacterAssignment(character.id);
            if (assignment != null)
            {
                itemUI.SetAssignedPlayer(assignment.assignedPlayerId, assignment.assignedUsername);
            }
        }
        else
        {
            Debug.LogWarning("DMCharacterAssignmentUI: CharacterAssignmentItemUI component missing on prefab");
        }
    }
    
    /// <summary>
    /// Get connected players formatted for dropdown options
    /// </summary>
    private List<(string playerId, string displayName)> GetConnectedPlayersForDropdown()
    {
        var options = new List<(string, string)>();
        options.Add(("", "-- Unassigned --")); // First option is unassigned
        
        if (PlayerConnectionManager.Instance == null) return options;
        
        var players = PlayerConnectionManager.Instance.GetConnectedPlayers(showHostInPlayerList);
        foreach (var player in players)
        {
            options.Add((player.playerId, player.username));
        }
        
        return options;
    }
    
    /// <summary>
    /// Refresh the connected players list UI
    /// </summary>
    private void RefreshConnectedPlayersList()
    {
        if (connectedPlayersContainer == null) return;
        
        // Clear existing
        foreach (Transform child in connectedPlayersContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (PlayerConnectionManager.Instance == null)
        {
            UpdateConnectedPlayersTitle(0);
            return;
        }
        
        var players = PlayerConnectionManager.Instance.GetConnectedPlayers(showHostInPlayerList);
        
        foreach (var player in players)
        {
            CreateConnectedPlayerItem(player);
        }
        
        UpdateConnectedPlayersTitle(players.Count);
        
        // Also update dropdowns in character items
        RefreshAllDropdowns();
    }
    
    /// <summary>
    /// Create a UI item showing a connected player
    /// </summary>
    private void CreateConnectedPlayerItem(ConnectedPlayer player)
    {
        if (connectedPlayerItemPrefab == null || connectedPlayersContainer == null) return;
        
        GameObject itemGO = Instantiate(connectedPlayerItemPrefab, connectedPlayersContainer);
        
        // Try to get a ConnectedPlayerItemUI component, or fall back to TextMeshPro
        var itemUI = itemGO.GetComponent<ConnectedPlayerItemUI>();
        if (itemUI != null)
        {
            // Check if this player has an assigned character
            var assignment = GetAssignmentForPlayer(player.playerId);
            string assignedCharName = assignment != null ? GetCharacterName(assignment.characterId) : null;
            itemUI.Setup(player, assignedCharName);
        }
        else
        {
            // Fallback: just set text if there's a TextMeshProUGUI
            var text = itemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                var assignment = GetAssignmentForPlayer(player.playerId);
                string status = assignment != null ? $" → {GetCharacterName(assignment.characterId)}" : "";
                text.text = $"{player.username}{status}";
            }
        }
    }
    
    /// <summary>
    /// Refresh all character assignment dropdowns with current player list
    /// </summary>
    private void RefreshAllDropdowns()
    {
        var dropdownOptions = GetConnectedPlayersForDropdown();
        foreach (var kvp in characterUIItems)
        {
            kvp.Value.UpdatePlayerOptions(dropdownOptions);
        }
    }
    
    private void UpdateConnectedPlayersTitle(int count)
    {
        if (connectedPlayersTitle != null)
        {
            connectedPlayersTitle.text = $"Connected Players ({count})";
        }
    }
    
    private void ShowEmptyState(string message)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = message;
        }
    }
    
    /// <summary>
    /// Called when an assignment is changed via the dropdown
    /// </summary>
    private void OnAssignmentChanged(string characterId, string playerId, string username)
    {
        Debug.Log($"DMCharacterAssignmentUI.OnAssignmentChanged: characterId={characterId}, playerId={playerId}, username={username}");
        
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host can assign characters");
            return;
        }
        
        if (string.IsNullOrEmpty(playerId))
        {
            // Unassign
            Debug.Log($"Unassigning character {characterId}");
            PlayerConnectionManager.Instance?.UnassignCharacter(characterId);
        }
        else
        {
            // Assign
            Debug.Log($"Assigning character {characterId} to player {username} (playerId: {playerId})");
            PlayerConnectionManager.Instance?.AssignPlayerToCharacter(characterId, playerId, username);
        }
    }
    
    // ========== EVENT HANDLERS ==========
    
    private void OnPlayerConnected(ConnectedPlayer player)
    {
        Debug.Log($"DMCharacterAssignmentUI: Player connected: {player.username}");
        RefreshConnectedPlayersList();
    }
    
    private void OnPlayerDisconnected(ulong clientId)
    {
        Debug.Log($"DMCharacterAssignmentUI: Player disconnected: {clientId}");
        RefreshConnectedPlayersList();
    }
    
    private void OnCharacterAssigned(string characterId, string playerId, string username)
    {
        Debug.Log($"DMCharacterAssignmentUI: Character {characterId} assigned to {username}");
        
        // Update the specific character item UI
        if (characterUIItems.TryGetValue(characterId, out var itemUI))
        {
            itemUI.SetAssignedPlayer(playerId, username);
        }
        
        RefreshConnectedPlayersList();
    }
    
    private void OnCharacterUnassigned(string characterId)
    {
        Debug.Log($"DMCharacterAssignmentUI: Character {characterId} unassigned");
        
        if (characterUIItems.TryGetValue(characterId, out var itemUI))
        {
            itemUI.SetAssignedPlayer("", "");
        }
        
        RefreshConnectedPlayersList();
    }
    
    // ========== HELPERS ==========
    
    private string GetPlayerCharactersFolder()
    {
        if (CampaignSelectionContext.HasSelection && !string.IsNullOrEmpty(CampaignSelectionContext.SelectedCampaignFilePath))
        {
            string campaignFolder = Path.GetDirectoryName(CampaignSelectionContext.SelectedCampaignFilePath);
            return Path.Combine(campaignFolder, "PlayerCharacters");
        }
        
        if (SceneDataTransfer.Instance != null)
        {
            string campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            if (!string.IsNullOrEmpty(campaignId))
            {
                string campaignsFolder = CampaignManager.GetCampaignsFolder();
                if (Directory.Exists(campaignsFolder))
                {
                    foreach (string folder in Directory.GetDirectories(campaignsFolder))
                    {
                        string campaignJsonPath = Path.Combine(folder, $"{campaignId}.json");
                        if (File.Exists(campaignJsonPath))
                        {
                            return Path.Combine(folder, "PlayerCharacters");
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    private CharacterPlayerAssignment GetAssignmentForPlayer(string playerId)
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.GetAssignmentForPlayer(playerId);
    }
    
    private string GetCharacterName(string characterId)
    {
        var character = campaignCharacters.Find(c => c.id == characterId);
        return character?.charName ?? characterId;
    }
}

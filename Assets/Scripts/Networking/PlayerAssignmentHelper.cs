using System;
using System.IO;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper component for clients/PCs to check their own character assignment.
/// Can be used to determine which character token the player controls.
/// Attach this to a persistent GameObject or use it as needed.
/// </summary>
public class PlayerAssignmentHelper : MonoBehaviour
{
    public static PlayerAssignmentHelper Instance { get; private set; }

    //Fields for setting up the players assigned character data so i can make specific pieces of info show up in the gameplay scene UI
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterClass;
    public TextMeshProUGUI characterLevel;
    public TextMeshProUGUI characterRace;
    public TextMeshProUGUI characterHP;
    public TextMeshProUGUI characterAC;
    public Image characterImg;
    
    // Event fired when the local player's assignment changes
    public event Action<CharacterData> OnMyAssignmentChanged;
    
    // Cached assignment data
    private CharacterPlayerAssignment myAssignment;
    private CharacterData myCharacter;
    
    // Cache for character data sent from server to avoid file loading on clients
    private System.Collections.Generic.Dictionary<string, CharacterData> characterDataCache = 
        new System.Collections.Generic.Dictionary<string, CharacterData>();
    
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
        
        // Auto-connect UI references if they're not already assigned
        ConnectUIReferences();
    }
    
    void OnEnable()
    {
        // Subscribe to assignment changes
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnCharacterAssigned += OnCharacterAssigned;
            PlayerConnectionManager.Instance.OnCharacterUnassigned += OnCharacterUnassigned;
        }
        
        // Try connecting UI references again in case they weren't available in Awake
        if (characterName == null)
        {
            ConnectUIReferences();
        }
    }
    
    void OnDisable()
    {
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnCharacterAssigned -= OnCharacterAssigned;
            PlayerConnectionManager.Instance.OnCharacterUnassigned -= OnCharacterUnassigned;
        }
    }
    
    /// <summary>
    /// Auto-connect UI references by searching the scene for PlayerCharDetails panel components
    /// </summary>
    private void ConnectUIReferences()
    {
        GameObject playerCharDetailsPanel = GameObject.Find("PlayerCharDetails");
        if (playerCharDetailsPanel == null) return;
        
        TryConnectTextComponent(playerCharDetailsPanel, "CharName", ref characterName);
        TryConnectTextComponent(playerCharDetailsPanel, "CharClass", ref characterClass);
        TryConnectTextComponent(playerCharDetailsPanel, "CharLevel", ref characterLevel);
        TryConnectTextComponent(playerCharDetailsPanel, "CharRace", ref characterRace);
        TryConnectTextComponent(playerCharDetailsPanel, "CharHP", ref characterHP);
        TryConnectTextComponent(playerCharDetailsPanel, "CharAC", ref characterAC);
        
        if (characterImg == null)
        {
            foreach (Image img in playerCharDetailsPanel.GetComponentsInChildren<Image>())
            {
                if (img.name.Contains("Character") || img.name.Contains("Portrait") || img.name.Contains("Token"))
                {
                    characterImg = img;
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Try to find and connect a TextMeshProUGUI component
    /// </summary>
    private void TryConnectTextComponent(GameObject parent, string componentName, ref TextMeshProUGUI field)
    {
        if (field != null) return;
        
        Transform found = parent.transform.Find(componentName);
        if (found != null)
        {
            TextMeshProUGUI tmpUGUI = found.GetComponent<TextMeshProUGUI>();
            if (tmpUGUI != null)
            {
                field = tmpUGUI;
                return;
            }
        }
        
        foreach (TextMeshProUGUI txt in parent.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (txt.name.Contains(componentName) || txt.name.ToLower().Contains(componentName.ToLower()))
            {
                field = txt;
                return;
            }
        }
    }
    
    /// <summary>
    /// Get the local player's Unity Authentication PlayerId
    /// </summary>
    public string GetMyPlayerId()
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
            Debug.LogWarning($"PlayerAssignmentHelper: Could not get PlayerId: {ex.Message}");
        }
        
        // Fallback
        return $"local_{SystemInfo.deviceUniqueIdentifier.Substring(0, 8)}";
    }
    
    /// <summary>
    /// Refresh and display the current character's UI
    /// Call this after scene loads if you want to display existing assignment
    /// </summary>
    public void RefreshCharacterDisplay()
    {
        Debug.Log("PlayerAssignmentHelper: Refreshing character display");
        myCharacter = GetMyCharacter();
        OnMyAssignmentChanged?.Invoke(myCharacter);
    }
    
    /// <summary>
    /// Check if the local player has a character assigned to them
    /// </summary>
    public bool HasAssignment()
    {
        return GetMyAssignment() != null;
    }
    
    /// <summary>
    /// Get the local player's character assignment
    /// </summary>
    public CharacterPlayerAssignment GetMyAssignment()
    {
        string myPlayerId = GetMyPlayerId();
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.GetAssignmentForPlayer(myPlayerId);
    }
    
    /// <summary>
    /// Get the CharacterData for the character assigned to the local player
    /// Loads character data from cache first (sent by server), then falls back to file loading
    /// </summary>
    public CharacterData GetMyCharacter()
    {
        var assignment = GetMyAssignment();
        if (assignment == null) return null;
        
        // First check the cache for character data sent from server
        if (characterDataCache.TryGetValue(assignment.characterId, out var cachedData))
        {
            Debug.Log($"Using cached character data for {assignment.characterId}");
            UpdateCharacterUI(cachedData);
            return cachedData;
        }
        
        // Fallback to loading from file (for offline or single-player scenarios)
        var characterData = LoadCharacterDataById(assignment.characterId);
        if (characterData != null)
        {
            // Update UI fields with the assigned character's data
            UpdateCharacterUI(characterData);
            // Cache it for future use
            characterDataCache[assignment.characterId] = characterData;
        }
        
        return characterData;
    }
    
    /// <summary>
    /// Receive and cache character data sent from the server, then save it locally
    /// Called when the host sends character data during assignment
    /// </summary>
    public void CacheCharacterData(string characterId, string characterDataJson)
    {
        if (string.IsNullOrEmpty(characterDataJson))
        {
            Debug.LogError($"CacheCharacterData: Received empty JSON for character {characterId}");
            return;
        }
        
        try
        {
            var characterData = JsonUtility.FromJson<CharacterData>(characterDataJson);
            if (characterData != null)
            {
                characterDataCache[characterId] = characterData;
                SaveCharacterDataLocally(characterId, characterDataJson);
            }
            else
            {
                Debug.LogError($"CacheCharacterData: Failed to deserialize character {characterId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"CacheCharacterData exception: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Receive and save character image data from host
    /// </summary>
    public void ReceiveCharacterImage(string characterId, string imageBase64)
    {
        try
        {
            if (string.IsNullOrEmpty(imageBase64)) return;
            
            byte[] imageData = System.Convert.FromBase64String(imageBase64);
            if (imageData == null || imageData.Length == 0) return;
            
            string campaignFolder = null;
            var campaign = CampaignManager.Instance?.GetCurrentCampaign();
            
            if (campaign != null)
            {
                campaignFolder = campaign.campaignName;
            }
            else if (SceneDataTransfer.Instance != null)
            {
                campaignFolder = SceneDataTransfer.Instance.GetCurrentCampaignId();
                if (string.IsNullOrEmpty(campaignFolder)) campaignFolder = "received";
            }
            else
            {
                campaignFolder = "received";
            }
            
            string imageFilename = null;
            if (characterDataCache.ContainsKey(characterId))
            {
                imageFilename = characterDataCache[characterId].tokenFileName;
            }
            
            if (string.IsNullOrEmpty(imageFilename)) return;
            
            string imagePath;
            #if UNITY_EDITOR
                imagePath = Path.Combine(Application.dataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters");
            #else
                imagePath = Path.Combine(Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters");
            #endif
            
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
            
            string fullImagePath = Path.Combine(imagePath, imageFilename);
            File.WriteAllBytes(fullImagePath, imageData);
        }
        catch { }
    }
    
    /// <summary>
    /// Save character data to ReceivedCampaigns folder for local access
    /// </summary>
    private void SaveCharacterDataLocally(string characterId, string characterDataJson)
    {
        try
        {
            string campaignFolder = null;
            var campaign = CampaignManager.Instance?.GetCurrentCampaign();
            
            if (campaign != null)
                campaignFolder = campaign.campaignName;
            else if (SceneDataTransfer.Instance != null)
            {
                campaignFolder = SceneDataTransfer.Instance.GetCurrentCampaignId();
                if (string.IsNullOrEmpty(campaignFolder)) campaignFolder = "received";
            }
            else
                campaignFolder = "received";
            
            string savePath;
            #if UNITY_EDITOR
                savePath = Path.Combine(Application.dataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters");
            #else
                savePath = Path.Combine(Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters");
            #endif
            
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            
            string characterFilePath = Path.Combine(savePath, $"{characterId}.json");
            File.WriteAllText(characterFilePath, characterDataJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveCharacterDataLocally failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load character data from a JSON file by character ID
    /// </summary>
    private CharacterData LoadCharacterDataById(string characterId)
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        if (campaign == null) return null;
        
        // Try ReceivedCampaigns first
        string receivedPath;
        #if UNITY_EDITOR
            receivedPath = Path.Combine(Application.dataPath, "Campaigns", "ReceivedCampaigns", campaign.campaignName, "Characters", $"{characterId}.json");
        #else
            receivedPath = Path.Combine(Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaign.campaignName, "Characters", $"{characterId}.json");
        #endif
        
        if (File.Exists(receivedPath))
        {
            try { return JsonUtility.FromJson<CharacterData>(File.ReadAllText(receivedPath)); }
            catch { }
        }
        
        // Fallback to local Campaigns
        string localPath = Path.Combine(Application.persistentDataPath, "Campaigns", campaign.campaignId, "PlayerCharacters", $"{characterId}.json");
        if (File.Exists(localPath))
        {
            try { return JsonUtility.FromJson<CharacterData>(File.ReadAllText(localPath)); }
            catch { }
        }
        
        return null;
    }
    
    /// <summary>
    /// Update the UI fields with character data
    /// </summary>
    private void UpdateCharacterUI(CharacterData characterData)
    {
        if (characterData == null) return;
        
        if (characterName != null) characterName.text = characterData.charName;
        if (characterClass != null) characterClass.text = characterData.charClass;
        if (characterLevel != null) characterLevel.text = $"Level {characterData.level}";
        if (characterRace != null) characterRace.text = characterData.race;
        if (characterHP != null) characterHP.text = $"{characterData.HP}";
        if (characterAC != null) characterAC.text = $"{characterData.AC}";
        
        if (characterImg == null || string.IsNullOrEmpty(characterData.tokenFileName)) return;
        
        Sprite sprite = Resources.Load<Sprite>($"CharacterTokens/{characterData.tokenFileName}");
        
        if (sprite == null)
        {
            var campaign = CampaignManager.Instance?.GetCurrentCampaign();
            string campaignFolder = campaign?.campaignName ?? (SceneDataTransfer.Instance?.GetCurrentCampaignId() ?? "received");
            
            string imagePath;
            #if UNITY_EDITOR
                imagePath = Path.Combine(Application.dataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters", characterData.tokenFileName);
            #else
                imagePath = Path.Combine(Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaignFolder, "Characters", characterData.tokenFileName);
            #endif
            
            if (File.Exists(imagePath))
            {
                byte[] imageData = File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
        }
        
        if (sprite != null) characterImg.sprite = sprite;
    }
    
    /// <summary>
    /// Get the character ID assigned to the local player
    /// </summary>
    public string GetMyCharacterId()
    {
        return GetMyAssignment()?.characterId;
    }
    
    /// <summary>
    /// Check if a specific character is assigned to the local player
    /// </summary>
    public bool IsMyCharacter(string characterId)
    {
        var assignment = GetMyAssignment();
        return assignment != null && assignment.characterId == characterId;
    }
    
    /// <summary>
    /// Check if the local player can control a specific token/character
    /// </summary>
    public bool CanControlCharacter(string characterId)
    {
        // Host/DM can control all characters
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            return true;
        }
        
        // Clients can only control their assigned character
        return IsMyCharacter(characterId);
    }
    
    /// <summary>
    /// Get all assignments (useful for displaying in UI)
    /// </summary>
    public System.Collections.Generic.List<CharacterPlayerAssignment> GetAllAssignments()
    {
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        return campaign?.characterAssignments?.assignments ?? new System.Collections.Generic.List<CharacterPlayerAssignment>();
    }
    
    // ========== EVENT HANDLERS ==========
    
    private void OnCharacterAssigned(string characterId, string playerId, string username)
    {
        if (playerId != GetMyPlayerId()) return;
        
        myAssignment = new CharacterPlayerAssignment(characterId, playerId, username);
        
        if (!characterDataCache.TryGetValue(characterId, out myCharacter))
            myCharacter = LoadCharacterDataById(characterId);
        
        if (myCharacter != null) UpdateCharacterUI(myCharacter);
        OnMyAssignmentChanged?.Invoke(myCharacter);
    }
    
    private void OnCharacterUnassigned(string characterId)
    {
        if (myAssignment?.characterId == characterId)
        {
            myAssignment = null;
            myCharacter = null;
            OnMyAssignmentChanged?.Invoke(null);
        }
    }
}

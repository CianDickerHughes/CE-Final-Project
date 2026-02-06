using System;
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
    
    void OnEnable()
    {
        // Subscribe to assignment changes
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnCharacterAssigned += OnCharacterAssigned;
            PlayerConnectionManager.Instance.OnCharacterUnassigned += OnCharacterUnassigned;
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
    /// </summary>
    public CharacterData GetMyCharacter()
    {
        var assignment = GetMyAssignment();
        if (assignment == null) return null;
        
        // Find the character in the campaign's player characters
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();
        if (campaign?.playerCharacters == null) return null;
        
        foreach (var pc in campaign.playerCharacters)
        {
            if (pc.characterData?.id == assignment.characterId)
            {
                //Updating the UI fields with those of the assigned characters data
                characterName.text = pc.characterData.charName;
                characterClass.text = pc.characterData.charClass;
                characterLevel.text = $"Level {pc.characterData.level}";
                characterRace.text = pc.characterData.race;
                characterHP.text = $"{pc.characterData.HP}";
                characterAC.text = $"{pc.characterData.AC}";
                characterImg.sprite = Resources.Load<Sprite>($"CharacterTokens/{pc.characterData.tokenFileName}");
                return pc.characterData;
            }
        }
        
        return null;
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
        string myPlayerId = GetMyPlayerId();
        
        if (playerId == myPlayerId)
        {
            Debug.Log($"PlayerAssignmentHelper: I was assigned to character {characterId}");
            
            // Cache and notify
            myAssignment = new CharacterPlayerAssignment(characterId, playerId, username);
            myCharacter = GetMyCharacter();
            OnMyAssignmentChanged?.Invoke(myCharacter);
        }
    }
    
    private void OnCharacterUnassigned(string characterId)
    {
        // Check if this was our character
        if (myAssignment != null && myAssignment.characterId == characterId)
        {
            Debug.Log($"PlayerAssignmentHelper: My character {characterId} was unassigned");
            
            myAssignment = null;
            myCharacter = null;
            OnMyAssignmentChanged?.Invoke(null);
        }
    }
}

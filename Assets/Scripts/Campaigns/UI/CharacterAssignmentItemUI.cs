using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single character in the assignment list.
/// Shows character info and a dropdown to select which player controls it.
/// </summary>
public class CharacterAssignmentItemUI : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterDetailsText;
    [SerializeField] private Image characterTokenImage;
    
    [Header("Assignment Controls")]
    [SerializeField] private TMP_Dropdown playerDropdown;
    [SerializeField] private TextMeshProUGUI assignedPlayerText;  // Alternative: just show text instead of dropdown
    [SerializeField] private Button unassignButton;
    
    [Header("Status Indicators")]
    [SerializeField] private GameObject assignedIndicator;   // Visual indicator when assigned
    [SerializeField] private GameObject unassignedIndicator; // Visual indicator when unassigned
    
    private CharacterData characterData;
    private string currentAssignedPlayerId = "";
    private List<(string playerId, string displayName)> playerOptions = new List<(string, string)>();
    private Action<string, string, string> onAssignmentChanged; // characterId, playerId, username
    
    /// <summary>
    /// Initialize this item with character data and available players
    /// </summary>
    public void Setup(CharacterData character, List<(string playerId, string displayName)> players, Action<string, string, string> onChanged)
    {
        characterData = character;
        playerOptions = players;
        onAssignmentChanged = onChanged;
        
        // Set character info
        if (characterNameText != null)
        {
            characterNameText.text = character.charName;
        }
        
        if (characterDetailsText != null)
        {
            characterDetailsText.text = $"{character.race} {character.charClass}";
        }
        
        // Load token image if available
        LoadTokenImage(character.tokenFileName);
        
        // Setup dropdown
        SetupDropdown();
        
        // Setup unassign button
        if (unassignButton != null)
        {
            unassignButton.onClick.AddListener(OnUnassignClicked);
            unassignButton.gameObject.SetActive(false); // Hidden until assigned
        }
        
        UpdateVisualState();
    }
    
    /// <summary>
    /// Update the available players in the dropdown
    /// </summary>
    public void UpdatePlayerOptions(List<(string playerId, string displayName)> players)
    {
        playerOptions = players;
        
        if (playerDropdown != null)
        {
            // Remember current selection
            string currentSelection = currentAssignedPlayerId;
            
            // Update dropdown options
            playerDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            int selectedIndex = 0;
            
            for (int i = 0; i < playerOptions.Count; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(playerOptions[i].displayName));
                if (playerOptions[i].playerId == currentSelection)
                {
                    selectedIndex = i;
                }
            }
            
            playerDropdown.AddOptions(options);
            playerDropdown.SetValueWithoutNotify(selectedIndex);
        }
    }
    
    /// <summary>
    /// Set the currently assigned player (from loaded data or network sync)
    /// </summary>
    public void SetAssignedPlayer(string playerId, string username)
    {
        currentAssignedPlayerId = playerId;
        
        // Update dropdown selection
        if (playerDropdown != null)
        {
            int index = 0; // Default to "Unassigned"
            for (int i = 0; i < playerOptions.Count; i++)
            {
                if (playerOptions[i].playerId == playerId)
                {
                    index = i;
                    break;
                }
            }
            playerDropdown.SetValueWithoutNotify(index);
        }
        
        // Update assigned player text (if using text display instead of/with dropdown)
        if (assignedPlayerText != null)
        {
            assignedPlayerText.text = string.IsNullOrEmpty(username) ? "Unassigned" : username;
        }
        
        UpdateVisualState();
    }
    
    private void SetupDropdown()
    {
        if (playerDropdown == null) return;
        
        playerDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var player in playerOptions)
        {
            options.Add(new TMP_Dropdown.OptionData(player.displayName));
        }
        
        playerDropdown.AddOptions(options);
        playerDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
    
    private void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= playerOptions.Count) return;
        
        var selected = playerOptions[index];
        currentAssignedPlayerId = selected.playerId;
        
        Debug.Log($"Character assignment changed: {characterData.charName} (id: {characterData.id}) -> {selected.displayName}");
        
        // Notify parent
        onAssignmentChanged?.Invoke(characterData.id, selected.playerId, selected.displayName);
        
        UpdateVisualState();
    }
    
    private void OnUnassignClicked()
    {
        currentAssignedPlayerId = "";
        
        // Reset dropdown to first option (unassigned)
        if (playerDropdown != null)
        {
            playerDropdown.SetValueWithoutNotify(0);
        }
        
        Debug.Log($"Character unassign clicked: {characterData.charName} (id: {characterData.id})");
        onAssignmentChanged?.Invoke(characterData.id, "", "");
        UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        bool isAssigned = !string.IsNullOrEmpty(currentAssignedPlayerId);
        
        if (assignedIndicator != null)
        {
            assignedIndicator.SetActive(isAssigned);
        }
        
        if (unassignedIndicator != null)
        {
            unassignedIndicator.SetActive(!isAssigned);
        }
        
        if (unassignButton != null)
        {
            unassignButton.gameObject.SetActive(isAssigned);
        }
    }
    
    private void LoadTokenImage(string tokenFileName)
    {
        if (characterTokenImage == null || string.IsNullOrEmpty(tokenFileName)) return;
        
        try
        {
            // Try to load from the same folder as the character JSON
            string playerCharactersFolder = GetPlayerCharactersFolder();
            if (!string.IsNullOrEmpty(playerCharactersFolder))
            {
                string tokenPath = System.IO.Path.Combine(playerCharactersFolder, tokenFileName);
                if (System.IO.File.Exists(tokenPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        characterTokenImage.sprite = Sprite.Create(
                            tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f)
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"CharacterAssignmentItemUI: Failed to load token image: {ex.Message}");
        }
    }
    
    private string GetPlayerCharactersFolder()
    {
        if (CampaignSelectionContext.HasSelection && !string.IsNullOrEmpty(CampaignSelectionContext.SelectedCampaignFilePath))
        {
            string campaignFolder = System.IO.Path.GetDirectoryName(CampaignSelectionContext.SelectedCampaignFilePath);
            return System.IO.Path.Combine(campaignFolder, "PlayerCharacters");
        }
        return null;
    }
}

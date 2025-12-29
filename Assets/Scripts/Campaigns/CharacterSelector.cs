using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This script handles the UI for players to select which character they want to use
//AFTER they've already connected to the campaign via netcode
//Potentially need to be removed/heavily altered later on once the networking is implemented/when functionality for character selection is finalized
public class CharacterSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform characterListContainer;
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Character Display")]
    [SerializeField] private TextMeshProUGUI selectedCharacterName;
    [SerializeField] private TextMeshProUGUI selectedCharacterDetails;
    [SerializeField] private Image selectedCharacterToken;
    
    private List<CharacterData> availableCharacters = new List<CharacterData>();
    private CharacterData selectedCharacter;
    
    void Start()
    {
        LoadPlayerCharacters();
        PopulateCharacterList();
        
        confirmButton.onClick.AddListener(OnConfirmCharacter);
        confirmButton.interactable = false;
    }
    
    // Load all characters saved on this device
    private void LoadPlayerCharacters()
    {
        availableCharacters.Clear();
        
        // Assuming you have a CharacterManager that saves characters to a specific location
        string charactersJson = PlayerPrefs.GetString("SavedCharacters", "");
        
        if (!string.IsNullOrEmpty(charactersJson))
        {
            // Parse the JSON array of characters
            CharacterDataList list = JsonUtility.FromJson<CharacterDataList>($"{{\"characters\":{charactersJson}}}");
            if (list != null && list.characters != null)
            {
                availableCharacters.AddRange(list.characters);
            }
        }
        
        if (availableCharacters.Count == 0)
        {
            statusText.text = "No characters found. Please create a character first.";
        }
        else
        {
            statusText.text = "Select a character for this campaign";
        }
    }
    
    // Display all available characters as selectable buttons
    private void PopulateCharacterList()
    {
        // Clear existing buttons
        foreach (Transform child in characterListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create a button for each character
        foreach (CharacterData character in availableCharacters)
        {
            GameObject buttonObj = Instantiate(characterButtonPrefab, characterListContainer);
            
            // Set up button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{character.charName}\n{character.race} {character.charClass}";
            }
            
            // Add click listener
            Button button = buttonObj.GetComponent<Button>();
            CharacterData currentChar = character; // Capture for closure
            button.onClick.AddListener(() => SelectCharacter(currentChar));
        }
    }
    
    // When player clicks on a character
    private void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        
        // Update UI to show selected character details
        selectedCharacterName.text = character.charName;
        selectedCharacterDetails.text = $"Race: {character.race}\n" +
                                        $"Class: {character.charClass}\n" +
                                        $"STR: {character.strength} | DEX: {character.dexterity}\n" +
                                        $"CON: {character.constitution} | INT: {character.intelligence}\n" +
                                        $"WIS: {character.wisdom} | CHA: {character.charisma}";
        
        // Load character token if available
        if (!string.IsNullOrEmpty(character.tokenFileName))
        {
            LoadCharacterToken(character.tokenFileName);
        }
        
        statusText.text = $"Selected: {character.charName}";
        confirmButton.interactable = true;
    }
    
    private void LoadCharacterToken(string fileName)
    {
        // Load the token image from persistent data path or resources
        string path = Path.Combine(Application.persistentDataPath, "CharacterTokens", fileName);
        
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                selectedCharacterToken.sprite = Sprite.Create(
                    tex, 
                    new Rect(0, 0, tex.width, tex.height), 
                    new Vector2(0.5f, 0.5f)
                );
            }
        }
    }
    
    // When player confirms their character choice
    private void OnConfirmCharacter()
    {
        if (selectedCharacter == null)
        {
            statusText.text = "Please select a character";
            return;
        }
        
        string username = PlayerPrefs.GetString("PlayerUsername", "Player");
        
        // Send the selected character to the host via network
        NetworkCharacterSync.Instance?.SendCharacterToHost(username, selectedCharacter);
        
        statusText.text = $"Character {selectedCharacter.charName} confirmed!";
        
        // Disable the confirm button so they can't send multiple times
        confirmButton.interactable = false;
        
        // Optionally close this UI and transition to the game
        // gameObject.SetActive(false);
        // GameSceneManager.Instance?.LoadGameScene();
    }
    
    // Public method to open this character selector
    // Called after the player has successfully connected to the network
    public void ShowCharacterSelection()
    {
        gameObject.SetActive(true);
        LoadPlayerCharacters();
        PopulateCharacterList();
    }
}

// Helper class for JSON deserialization of character arrays
[System.Serializable]
public class CharacterDataList
{
    public List<CharacterData> characters;
}
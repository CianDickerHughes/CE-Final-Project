
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This script provides the DM with an interface to manage which characters are in which scenes
//Potentially remove it later but for now it helps with testing scene management

public class DMSceneManager : MonoBehaviour
{
    [Header("Scene Selection")]
    [SerializeField] private TMP_Dropdown sceneDropdown;
    [SerializeField] private TextMeshProUGUI sceneInfoText;
    
    [Header("Available Players")]
    [SerializeField] private Transform availablePlayersContainer;
    [SerializeField] private GameObject playerCharacterItemPrefab;
    
    [Header("Characters in Scene")]
    [SerializeField] private Transform activeCharactersContainer;
    [SerializeField] private GameObject activeCharacterItemPrefab;
    
    private Campaign currentCampaign;
    private SceneData currentScene;
    
    void Start()
    {
        currentCampaign = CampaignManager.Instance.GetCurrentCampaign();
        
        if (currentCampaign == null)
        {
            Debug.LogError("No active campaign!");
            return;
        }
        
        PopulateSceneDropdown();
        sceneDropdown.onValueChanged.AddListener(OnSceneSelected);
        
        //Load the first scene by default
        if (currentCampaign.scenes.Count > 0)
        {
            OnSceneSelected(0);
        }
    }
    
    //Populate dropdown with all scenes in the campaign
    private void PopulateSceneDropdown()
    {
        sceneDropdown.ClearOptions();
        
        List<string> sceneNames = new List<string>();
        foreach (SceneData scene in currentCampaign.scenes)
        {
            sceneNames.Add($"{scene.sceneName} ({scene.sceneType})");
        }
        
        sceneDropdown.AddOptions(sceneNames);
    }
    
    //When DM selects a different scene
    private void OnSceneSelected(int index)
    {
        if (index < 0 || index >= currentCampaign.scenes.Count)
            return;
            
        currentScene = currentCampaign.scenes[index];
        
        //Update scene info
        sceneInfoText.text = $"Scene: {currentScene.sceneName}\n" + $"Type: {currentScene.sceneType}\n" + $"Description: {currentScene.description}";
        
        RefreshPlayerLists();
    }
    
    //Refresh both the available players list and active characters list
    private void RefreshPlayerLists()
    {
        PopulateAvailablePlayers();
        PopulateActiveCharacters();
    }
    
    //Show all players who have joined the campaign
    private void PopulateAvailablePlayers()
    {
        //Clear existing items
        foreach (Transform child in availablePlayersContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Create an item for each player
        foreach (PlayerCharacterAssignment player in currentCampaign.playerCharacters)
        {
            GameObject itemObj = Instantiate(playerCharacterItemPrefab, availablePlayersContainer);
            
            //Display player info
            TextMeshProUGUI infoText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (infoText != null)
            {
                infoText.text = $"{player.playerUsername}\n" +
                               $"{player.characterData.charName} ({player.characterData.charClass})";
            }
            
            //Add button to add this character to the scene
            Button addButton = itemObj.GetComponentInChildren<Button>();
            if (addButton != null)
            {
                string characterId = player.characterData.id;
                addButton.onClick.AddListener(() => AddCharacterToCurrentScene(characterId));
                
                //Disable button if character is already in scene
                bool isInScene = currentScene.activeCharacterIds.Contains(characterId);
                addButton.interactable = !isInScene;
                
                TextMeshProUGUI buttonText = addButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isInScene ? "In Scene" : "Add to Scene";
                }
            }
        }
    }
    
    //Show all characters currently in the selected scene
    private void PopulateActiveCharacters()
    {
        //Clear existing items
        foreach (Transform child in activeCharactersContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Get all characters in this scene
        List<CharacterData> charactersInScene = CampaignManager.Instance.GetCharactersInScene(currentScene.sceneId);
        
        foreach (CharacterData character in charactersInScene)
        {
            GameObject itemObj = Instantiate(activeCharacterItemPrefab, activeCharactersContainer);
            
            //Find the player who owns this character
            PlayerCharacterAssignment player = currentCampaign.playerCharacters
                .Find(p => p.characterData.id == character.id);
            
            //Display character info
            TextMeshProUGUI infoText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (infoText != null)
            {
                infoText.text = $"{character.charName}\n" + $"Player: {player?.playerUsername ?? "Unknown"}\n" + $"HP: {character.constitution * 10}";
            }
            
            //Add button to remove character from scene
            Button removeButton = itemObj.GetComponentInChildren<Button>();
            if (removeButton != null)
            {
                string characterId = character.id;
                removeButton.onClick.AddListener(() => RemoveCharacterFromCurrentScene(characterId));
                
                TextMeshProUGUI buttonText = removeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Remove";
                }
            }
        }
    }
    
    //Add a character to the current scene
    private void AddCharacterToCurrentScene(string characterId)
    {
        bool success = CampaignManager.Instance.AddCharacterToScene(currentScene.sceneId, characterId);
        
        if (success)
        {
            RefreshPlayerLists();
            
            //Optionally, network sync this change to all clients
            //NetworkSceneSync.Instance?.SyncCharacterAdded(currentScene.sceneId, characterId);
        }
    }
    
    //Remove a character from the current scene
    private void RemoveCharacterFromCurrentScene(string characterId)
    {
        bool success = CampaignManager.Instance.RemoveCharacterFromScene(currentScene.sceneId, characterId);
        
        if (success)
        {
            RefreshPlayerLists();
            
            //Optionally, network sync this change to all clients
            //NetworkSceneSync.Instance?.SyncCharacterRemoved(currentScene.sceneId, characterId);
        }
    }
    
    //Refresh button for manual updates
    public void OnRefreshClicked()
    {
        currentCampaign = CampaignManager.Instance.GetCurrentCampaign();
        if (currentCampaign != null)
        {
            RefreshPlayerLists();
        }
    }
}
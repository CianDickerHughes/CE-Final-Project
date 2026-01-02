using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

//MAIN MANAGEMENT FOR CAMPAIGNS
//This class will act as a manager for the various actions to do with campaigns - creating them, adding players, adding scenes etc.
//Effectively outlines main functionality for stuff to do with campaigns - excluding the actual data structures themselves, 
//IMPORTANT THING TO REMEMBER: USE THE SAVE() METHOD AFTER ANY CHANGES TO CAMPAIGN DATA TO ENSURE IT IS SAVED PROPERLY -although this may need to be changed later on

public class CampaignManager : MonoBehaviour
{

    public static CampaignManager Instance { get; private set; }
    private Campaign currentCampaign;
    //For the sake of space concerns we restrict the number of scenes per campaign to 6 for now
    private const int MAX_SCENES = 6;
    
    void Awake()
    {
        //This is just us initializing the singleton for this class
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
    
    //MAIN CAMPAIGN FUNCTIONS
    //Create a new campaign - this new way does it without the id's 
    //We will generate a new campaign ID within the Campaign class itself
    public Campaign CreateCampaign(string campaignName, string dmUsername, string description = "")
    {
        currentCampaign = new Campaign(campaignName, dmUsername, description);
        SaveCampaign();
        Debug.Log($"Campaign created: {campaignName} with code: {currentCampaign.inviteCode}");
        return currentCampaign;
    }
    
    //HOST ONLY: Add a player's character to the current campaign
    //This is called when a client sends their character data over the network
    public bool AddPlayerCharacterToCampaign(string playerUsername, ulong networkId, CharacterData character)
    {
        if (currentCampaign == null)
        {
            Debug.LogError("No active campaign on host!");
            return false;
        }
        
        //We then just assign the character to the player within the campaign
        currentCampaign.AssignCharacterToPlayer(playerUsername, networkId, character);
        //Then make sure to save the campaign data
        SaveCampaign();
        Debug.Log($"Player {playerUsername} joined with character {character.charName}");
        return true;
    }
    
    //Update a player's character selection (if they want to switch before starting)
    public bool UpdatePlayerCharacter(ulong networkId, CharacterData newCharacter)
    {
        if (currentCampaign == null)
        {
            return false;
        }
        
        //Finding the player's assignment and updating their character data - done using network ID
        var assignment = currentCampaign.playerCharacters.Find(p => p.networkId == networkId);
        if (assignment != null)
        {
            assignment.characterData = newCharacter;
            //NEED TO KEEP REMEMBERING TO SAVE THE CAMPAIGN AFTER CHANGES
            SaveCampaign();
            Debug.Log($"Player character updated to {newCharacter.charName}");
            return true;
        }
        return false;
    }
    
    //Add a new scene to the current campaign
    public bool AddScene(string sceneName, SceneType sceneType, string description)
    {
        if (currentCampaign == null)
        {
            Debug.LogError("No active campaign!");
            return false;
        }
        
        //Checking we are not exceeding the max scenes limit
        if (currentCampaign.scenes.Count >= MAX_SCENES)
        {
            Debug.LogWarning("Maximum scene limit reached (5 scenes)");
            return false;
        }
        //If not then we can add the new scene
        SceneData newScene = new SceneData(sceneName, sceneType, description);
        currentCampaign.scenes.Add(newScene);
        SaveCampaign();
        Debug.Log($"Scene added: {sceneName}");
        return true;
    }
    
    //Remove a scene from the current campaign
    public bool RemoveScene(string sceneId)
    {
        if (currentCampaign == null)
            return false;
        
        int removed = currentCampaign.scenes.RemoveAll(s => s.sceneId == sceneId);
        if (removed > 0)
        {
            SaveCampaign();
            Debug.Log("Scene removed");
            return true;
        }
        return false;
    }
    
    //DM adds a player's character to a specific scene - potentially usefull for specific scenarios
    //Or just setting up combat encounters etc.
    public bool AddCharacterToScene(string sceneId, string characterId)
    {
        if (currentCampaign == null)
            return false;
            
        SceneData scene = currentCampaign.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            scene.AddCharacterToScene(characterId);
            SaveCampaign();
            Debug.Log($"Character added to scene {scene.sceneName}");
            return true;
        }
        return false;
    }
    
    //DM removes a character from a scene
    public bool RemoveCharacterFromScene(string sceneId, string characterId)
    {
        if (currentCampaign == null)
            return false;
            
        SceneData scene = currentCampaign.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            scene.RemoveCharacterFromScene(characterId);
            SaveCampaign();
            Debug.Log($"Character removed from scene {scene.sceneName}");
            return true;
        }
        return false;
    }
    
    //Get all characters in a specific scene
    //Listing out characters in a scene - good for showing who is present in a scene (This would be purely UI for the DM)
    public List<CharacterData> GetCharactersInScene(string sceneId)
    {
        List<CharacterData> characters = new List<CharacterData>();
        
        if (currentCampaign == null)
            return characters;
            
        SceneData scene = currentCampaign.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            foreach (string charId in scene.activeCharacterIds)
            {
                CharacterData character = currentCampaign.playerCharacters
                    .Find(p => p.characterData.id == charId)?.characterData;
                if (character != null)
                {
                    characters.Add(character);
                }
            }
        }
        
        return characters;
    }
    
    //Get the character controlled by a specific network player
    public CharacterData GetCharacterForPlayer(ulong networkId)
    {
        return currentCampaign?.GetCharacterForPlayer(networkId);
    }
    
    // Get all players in the current campaign
    public List<PlayerCharacterAssignment> GetAllPlayers()
    {
        return currentCampaign?.playerCharacters ?? new List<PlayerCharacterAssignment>();
    }
    
    public Campaign GetCurrentCampaign()
    {
        return currentCampaign;
    }
    
    public int GetSceneCount()
    {
        return currentCampaign?.scenes.Count ?? 0;
    }
    
    public bool CanAddScene()
    {
        return currentCampaign != null && currentCampaign.scenes.Count < MAX_SCENES;
    }
    
    //Save the current campaign
    public void SaveCampaign()
    {
        if (currentCampaign != null)
        {
            string json = JsonUtility.ToJson(currentCampaign, true);
            string folderPath = Path.Combine(Application.dataPath, "Campaigns");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, $"{currentCampaign.campaignId}.json");
            File.WriteAllText(filePath, json);
            Debug.Log($"Campaign saved to {filePath}");
        }
    }
    
    //Load a campaign by ID
    public Campaign LoadCampaign(string campaignId)
    {
        string folderPath = Path.Combine(Application.dataPath, "Campaigns");
        string filePath = Path.Combine(folderPath, $"{campaignId}.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            currentCampaign = JsonUtility.FromJson<Campaign>(json);
            return currentCampaign;
        }
        return null;
    }
    
    //Load the last active campaign
    public Campaign LoadLastCampaign()
    {
        //Placeholder - in future we may want to store the last opened campaign ID in player prefs or similar
        return null;
    }

    // Returns the folder path to save campaigns
    public static string GetCampaignsFolder()
    {
        #if UNITY_EDITOR
            string folder = Path.Combine(Application.dataPath, "Campaigns");
        #else
            string folder = Path.Combine(Application.persistentDataPath, "Campaigns");
        #endif
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return folder;
    }
}
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

//Manages characters - saves them to UserData folder
//Filters to show only the current user's characters
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }
    
    private string characterDataPath;
    private const string CHARACTER_FILE = "characters.json";
    private List<CharacterData> allCharacters = new List<CharacterData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            //Set up path to UserData folder
            characterDataPath = Path.Combine(Application.dataPath, "UserData");
            
            //Create folder if it doesn't exist
            if (!Directory.Exists(characterDataPath))
            {
                Directory.CreateDirectory(characterDataPath);
            }
            
            LoadAllCharacters();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    //Create a new character (auto-assigns to current user)
    public CharacterData CreateCharacter(string name, string race, string charClass)
    {
        if (!SessionManager.Instance.IsLoggedIn)
        {
            Debug.LogError("Must be logged in to create a character");
            return null;
        }
        
        CharacterData newChar = new CharacterData
        {
            charName = name,
            race = race,
            charClass = charClass,
            ownerUsername = SessionManager.Instance.CurrentUsername
        };
        
        allCharacters.Add(newChar);
        SaveAllCharacters();
        
        Debug.Log($"Character '{name}' created for user '{newChar.ownerUsername}'");
        return newChar;
    }
    
    //Get only the current user's characters
    public List<CharacterData> GetCurrentUserCharacters()
    {
        if (!SessionManager.Instance.IsLoggedIn)
        {
            return new List<CharacterData>();
        }
        
        string currentUser = SessionManager.Instance.CurrentUsername;
        
        return allCharacters
            .Where(c => c.ownerUsername == currentUser)
            .ToList();
    }
    
    //Get a specific character by ID (only if owned by current user)
    public CharacterData GetCharacterById(string characterId)
    {
        CharacterData character = allCharacters.FirstOrDefault(c => c.id == characterId);
        
        if (character != null && character.IsOwnedByCurrentUser())
        {
            return character;
        }
        
        return null;
    }
    
    //Update an existing character
    public bool UpdateCharacter(CharacterData updatedCharacter)
    {
        int index = allCharacters.FindIndex(c => c.id == updatedCharacter.id);
        
        if (index == -1)
        {
            return false;
        }
        
        if (!updatedCharacter.IsOwnedByCurrentUser())
        {
            Debug.LogError("Cannot update character you don't own");
            return false;
        }
        
        allCharacters[index] = updatedCharacter;
        SaveAllCharacters();
        return true;
    }
    
    //Delete a character (only if owned by current user)
    public bool DeleteCharacter(string characterId)
    {
        CharacterData character = GetCharacterById(characterId);
        
        if (character == null)
        {
            return false;
        }
        
        allCharacters.Remove(character);
        SaveAllCharacters();
        return true;
    }
    
    //Save all characters to file
    private void SaveAllCharacters()
    {
        string filePath = Path.Combine(characterDataPath, CHARACTER_FILE);
        
        CharacterDataList dataList = new CharacterDataList
        {
            characters = allCharacters
        };
        
        string json = JsonUtility.ToJson(dataList, true);
        
        try
        {
            File.WriteAllText(filePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving characters: {e.Message}");
        }
    }
    
    //Load all characters from file
    private void LoadAllCharacters()
    {
        string filePath = Path.Combine(characterDataPath, CHARACTER_FILE);
        
        if (!File.Exists(filePath))
        {
            allCharacters = new List<CharacterData>();
            return;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            CharacterDataList dataList = JsonUtility.FromJson<CharacterDataList>(json);
            
            if (dataList != null && dataList.characters != null)
            {
                allCharacters = dataList.characters;
                Debug.Log($"Loaded {allCharacters.Count} characters");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading characters: {e.Message}");
            allCharacters = new List<CharacterData>();
        }
    }
}
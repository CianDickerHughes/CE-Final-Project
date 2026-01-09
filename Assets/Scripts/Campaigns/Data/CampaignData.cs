using System;
using System.Collections.Generic;
using UnityEngine;

//This class will handle/encapsulate all basic data relating to campaigns in general
//e.g. the campaign name, who is the dm, a list of the players, a list of scenes etc.

[System.Serializable]
public class Campaign
{
    public string campaignId;
    public string campaignName;
    public string campaignDescription;
    public string dmUsername;
    public List<PlayerCharacterAssignment> playerCharacters;
    public List<SceneData> scenes;
    public string inviteCode;
    public string createdDate;
    public string campaignLogoPath;
    
    //Constructor for the main source of initialization
    public Campaign(string name, string dmName, string description = "")
    {
        campaignId = Guid.NewGuid().ToString();
        campaignName = name;
        campaignDescription = description;
        dmUsername = dmName;
        playerCharacters = new List<PlayerCharacterAssignment>();
        scenes = new List<SceneData>();
        inviteCode = GenerateInviteCode();
        createdDate = DateTime.Now.ToString("o");
    }
    
    //Generating the invite code to send to the players to actually join
    //NOTE - THIS WILL MOST DEFINETLY CHANGE LATER AS WE ADD IN UNITY RELAY/MULTIPLAYER/NETCODE
    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        return new string(code);
    }
    
    //Basic method at the moment to assign a character to a player within the campaign
    //This will work with the Unity Netcode system later on when we integrate multiplayer properly
    //Basically: PlayerUsername + NetworkID (from netcode) + CharacterData - all linked together and stored here
    public void AssignCharacterToPlayer(string playerUsername, ulong networkId, CharacterData character)
    {
        // Check if player already has a character assigned
        var existing = playerCharacters.Find(p => p.networkId == networkId);
        if (existing != null)
        {
            existing.characterData = character;
            existing.playerUsername = playerUsername;
        }
        else
        {
            playerCharacters.Add(new PlayerCharacterAssignment
            {
                playerUsername = playerUsername,
                networkId = networkId,
                characterData = character
            });
        }
    }
    
    //Simple getter method for getting a player's character by their network ID
    public CharacterData GetCharacterForPlayer(ulong networkId)
    {
        return playerCharacters.Find(p => p.networkId == networkId)?.characterData;
    }
    
    //Removing a player from the campaign
    //POTENTIALLY REMOVED LATER SINCE WE MAY NOT BE STORING PLAYERS AS OPPOSED TO CHARACTERS
    public bool RemovePlayer(ulong networkId)
    {
        return playerCharacters.RemoveAll(p => p.networkId == networkId) > 0;
    }
}

//New class to link players to their characters within a campaign
[System.Serializable]
public class PlayerCharacterAssignment
{
    public string playerUsername;
    //Network ID from Unity Netcode to identify the player
    //Ulong is what Netcode uses for NetworkObject IDs in Unity
    public ulong networkId;
    public CharacterData characterData;
}

//Class to handle data relating to individual scenes within a campaign
[System.Serializable]
public class SceneData
{
    public string sceneId;
    public string sceneName;
    public SceneType sceneType;
    public string description;
    public string lastPlayed;
    public string status;
    public MapData mapData;
    //This is to help us keep track of which characters are active in this scene - i.e. who is playing or what players are here
    public List<string> activeCharacterIds;
    
    public SceneData(string name, SceneType type, string desc)
    {
        sceneId = Guid.NewGuid().ToString();
        sceneName = name;
        sceneType = type;
        description = desc;
        lastPlayed = "";
        status = "Not Started";
        mapData = new MapData();
        activeCharacterIds = new List<string>();
    }
    
    //Add a character to this scene
    public void AddCharacterToScene(string characterId)
    {
        if (!activeCharacterIds.Contains(characterId))
        {
            activeCharacterIds.Add(characterId);
        }
    }
    
    //Remove a character from this scene
    public void RemoveCharacterFromScene(string characterId)
    {
        activeCharacterIds.Remove(characterId);
    }
}

//Holds the map/grid layout data for a scene.
//This is what gets saved/loaded when the DM creates a map in the Scene Maker.
//Stores tile types as integers for easy JSON serialization.
[System.Serializable]
public class MapData
{
    public int width;
    public int height;
    public int[] tiles; // Flattened 2D array of TileType values
    public string createdDate;
    public string lastModified;

    //Default constructor - creates an empty 15x15 map.
    public MapData()
    {
        width = 15;
        height = 15;
        tiles = new int[width * height];
        // Initialize all tiles to Floor by default
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastModified = createdDate;
    }

    //Create a map with specific dimensions.
    public MapData(int mapWidth, int mapHeight)
    {
        width = mapWidth;
        height = mapHeight;
        tiles = new int[width * height];
        // Initialize all tiles to Floor by default
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastModified = createdDate;
    }

    //Get the tile type at a specific grid position.
    //Returns Empty if out of bounds.
    public TileType GetTileAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return TileType.Empty;

        int index = y * width + x;
        return (TileType)tiles[index];
    }

    //Set the tile type at a specific grid position.
    public void SetTileAt(int x, int y, TileType type)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;
        tiles[index] = (int)type;
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Resize the map, preserving existing tiles where possible.
    public void Resize(int newWidth, int newHeight)
    {
        int[] newTiles = new int[newWidth * newHeight];
        
        // Initialize new tiles to Floor
        for (int i = 0; i < newTiles.Length; i++)
        {
            newTiles[i] = (int)TileType.Floor;
        }

        // Copy existing tiles that fit in new dimensions
        int copyWidth = Mathf.Min(width, newWidth);
        int copyHeight = Mathf.Min(height, newHeight);

        for (int y = 0; y < copyHeight; y++)
        {
            for (int x = 0; x < copyWidth; x++)
            {
                int oldIndex = y * width + x;
                int newIndex = y * newWidth + x;
                newTiles[newIndex] = tiles[oldIndex];
            }
        }

        width = newWidth;
        height = newHeight;
        tiles = newTiles;
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Clear all tiles back to Floor.
    public void Clear()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Fill the entire map with a specific tile type.
    public void Fill(TileType type)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)type;
        }
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Check if a position is within the map bounds.
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //Check if a position is walkable (valid and not blocked).
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;

        TileType type = GetTileAt(x, y);
        return TileTypeProperties.IsWalkable(type);
    }
}

//Enum for the different types of scenes we may have in a campaign
//For now it will just be these 3 but we can flexibly add more later on - probably wont though
public enum SceneType
{
    Roleplay,
    Combat,
    Exploration
}
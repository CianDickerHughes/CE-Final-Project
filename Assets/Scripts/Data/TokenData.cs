using UnityEngine;
using System;
using System.Collections.Generic;

//Serializable class to store token position and identification data for saving/loading
[System.Serializable]
public class TokenData
{
    public string characterId;  // ID of the character (for player/NPC tokens)
    public string characterName; // Name of the character
    public string characterClass; // Class of the character
    public string tokenFileName; // Filename of the token image
    public string characterDescription; // Description/race of the character
    public string enemyId;      // ID of the enemy (for enemy tokens)
    public CharacterType tokenType;  // Player, NPC, or Enemy
    public int gridX;           // X position on grid
    public int gridY;           // Y position on grid
    
    public TokenData()
    {
        characterId = "";
        characterName = "";
        characterClass = "";
        tokenFileName = "";
        characterDescription = "";
        enemyId = "";
        tokenType = CharacterType.Player;
        gridX = 0;
        gridY = 0;
    }
    
    public TokenData(string charId, string enemyIdVal, CharacterType type, int x, int y)
    {
        characterId = charId ?? "";
        characterName = "";
        characterClass = "";
        tokenFileName = "";
        characterDescription = "";
        enemyId = enemyIdVal ?? "";
        tokenType = type;
        gridX = x;
        gridY = y;
    }
    
    // Full constructor with all character data for network transmission
    public TokenData(string charId, string charName, string charClass, string tokenFile, string charDesc, string enemyIdVal, CharacterType type, int x, int y)
    {
        characterId = charId ?? "";
        characterName = charName ?? "";
        characterClass = charClass ?? "";
        tokenFileName = tokenFile ?? "";
        characterDescription = charDesc ?? "";
        enemyId = enemyIdVal ?? "";
        tokenType = type;
        gridX = x;
        gridY = y;
    }
}

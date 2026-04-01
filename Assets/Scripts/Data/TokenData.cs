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
    public int hp;
    public int ac;
    
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
        hp = 0;
        ac = 0;
    }
    
    public TokenData(string charId, string enemyIdVal, CharacterType type, int x, int y, int hpVal, int acVal)
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
        hp = hpVal;
        ac = acVal;
    }
    
    // Full constructor with all character data for network transmission
    public TokenData(string charId, string charName, string charClass, string tokenFile, string charDesc, string enemyIdVal, CharacterType type, int x, int y, int hpVal, int acVal)
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
        hp = hpVal;
        ac = acVal;
    }
}

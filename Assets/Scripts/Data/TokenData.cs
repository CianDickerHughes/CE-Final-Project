using UnityEngine;
using System;
using System.Collections.Generic;

//Serializable class to store token position and identification data for saving/loading
[System.Serializable]
public class TokenData
{
    public string characterId;  // ID of the character (for player/NPC tokens)
    public string enemyId;      // ID of the enemy (for enemy tokens)
    public CharacterType tokenType;  // Player, NPC, or Enemy
    public int gridX;           // X position on grid
    public int gridY;           // Y position on grid
    
    public TokenData()
    {
        characterId = "";
        enemyId = "";
        tokenType = CharacterType.Player;
        gridX = 0;
        gridY = 0;
    }
    
    public TokenData(string charId, string enemyIdVal, CharacterType type, int x, int y)
    {
        characterId = charId ?? "";
        enemyId = enemyIdVal ?? "";
        tokenType = type;
        gridX = x;
        gridY = y;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a connected player in the current network session.
/// This tracks the live connection info separate from persistent character assignments.
/// </summary>
[System.Serializable]
public class ConnectedPlayer
{
    public ulong clientId;           // Network client ID from Netcode
    public string username;          // Player's username
    public string playerId;          // Unity Authentication player ID (persistent across sessions)
    public bool isHost;              // Whether this player is the host/DM
    public string connectedAt;       // Timestamp when player connected
    
    public ConnectedPlayer()
    {
        connectedAt = DateTime.Now.ToString("o");
    }
    
    public ConnectedPlayer(ulong clientId, string username, string playerId, bool isHost = false)
    {
        this.clientId = clientId;
        this.username = username;
        this.playerId = playerId;
        this.isHost = isHost;
        this.connectedAt = DateTime.Now.ToString("o");
    }
}

/// <summary>
/// Tracks which player was assigned to which character for a campaign.
/// Uses Unity Authentication PlayerId for persistence across sessions.
/// Stored in the Campaign data so assignments are remembered.
/// </summary>
[System.Serializable]
public class CharacterPlayerAssignment
{
    public string characterId;       // The character's unique ID
    public string assignedPlayerId;  // Unity Authentication PlayerId (persistent)
    public string assignedUsername;  // Username for display purposes
    public string lastAssignedAt;    // When this assignment was last made
    
    public CharacterPlayerAssignment()
    {
        lastAssignedAt = DateTime.Now.ToString("o");
    }
    
    public CharacterPlayerAssignment(string characterId, string playerId, string username)
    {
        this.characterId = characterId;
        this.assignedPlayerId = playerId;
        this.assignedUsername = username;
        this.lastAssignedAt = DateTime.Now.ToString("o");
    }
}

/// <summary>
/// Container for storing all character-to-player assignments for a campaign.
/// This is stored within the Campaign data.
/// </summary>
[System.Serializable]
public class CampaignCharacterAssignments
{
    public List<CharacterPlayerAssignment> assignments = new List<CharacterPlayerAssignment>();
    
    /// <summary>
    /// Get the assignment for a specific character.
    /// </summary>
    public CharacterPlayerAssignment GetAssignmentForCharacter(string characterId)
    {
        return assignments.Find(a => a.characterId == characterId);
    }
    
    /// <summary>
    /// Get the assignment for a specific player (by PlayerId).
    /// </summary>
    public CharacterPlayerAssignment GetAssignmentForPlayer(string playerId)
    {
        return assignments.Find(a => a.assignedPlayerId == playerId);
    }
    
    /// <summary>
    /// Assign a player to a character. Updates existing or creates new assignment.
    /// </summary>
    public void AssignPlayerToCharacter(string characterId, string playerId, string username)
    {
        // Remove any existing assignment for this character
        assignments.RemoveAll(a => a.characterId == characterId);
        
        // Remove any existing assignment for this player (a player can only control one character)
        assignments.RemoveAll(a => a.assignedPlayerId == playerId);
        
        // Create new assignment
        assignments.Add(new CharacterPlayerAssignment(characterId, playerId, username));
    }
    
    /// <summary>
    /// Remove assignment for a character.
    /// </summary>
    public void UnassignCharacter(string characterId)
    {
        assignments.RemoveAll(a => a.characterId == characterId);
    }
    
    /// <summary>
    /// Remove all assignments for a player.
    /// </summary>
    public void UnassignPlayer(string playerId)
    {
        assignments.RemoveAll(a => a.assignedPlayerId == playerId);
    }
    
    /// <summary>
    /// Check if a character is currently assigned to any player.
    /// </summary>
    public bool IsCharacterAssigned(string characterId)
    {
        return assignments.Exists(a => a.characterId == characterId);
    }
    
    /// <summary>
    /// Check if a player has any character assigned.
    /// </summary>
    public bool IsPlayerAssigned(string playerId)
    {
        return assignments.Exists(a => a.assignedPlayerId == playerId);
    }
}

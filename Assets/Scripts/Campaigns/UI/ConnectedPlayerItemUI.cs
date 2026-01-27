using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying a connected player in the players list.
/// Shows the player's name and which character they are assigned to (if any).
/// </summary>
public class ConnectedPlayerItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI assignedCharacterText;
    [SerializeField] private Image playerIcon;
    
    [Header("Status Indicators")]
    [SerializeField] private GameObject assignedIndicator;   // Shows when player has a character
    [SerializeField] private GameObject unassignedIndicator; // Shows when player needs a character
    [SerializeField] private Image statusColor;              // Color indicator
    
    [Header("Colors")]
    [SerializeField] private Color assignedColor = new Color(0.2f, 0.8f, 0.2f, 1f);   // Green
    [SerializeField] private Color unassignedColor = new Color(0.8f, 0.8f, 0.2f, 1f); // Yellow
    
    private ConnectedPlayer playerData;
    
    /// <summary>
    /// Setup this item with player info
    /// </summary>
    public void Setup(ConnectedPlayer player, string assignedCharacterName = null)
    {
        playerData = player;
        
        if (usernameText != null)
        {
            usernameText.text = player.username;
        }
        
        bool hasAssignment = !string.IsNullOrEmpty(assignedCharacterName);
        
        if (assignedCharacterText != null)
        {
            assignedCharacterText.text = hasAssignment ? $"Playing: {assignedCharacterName}" : "No character assigned";
            assignedCharacterText.color = hasAssignment ? assignedColor : unassignedColor;
        }
        
        if (assignedIndicator != null)
        {
            assignedIndicator.SetActive(hasAssignment);
        }
        
        if (unassignedIndicator != null)
        {
            unassignedIndicator.SetActive(!hasAssignment);
        }
        
        if (statusColor != null)
        {
            statusColor.color = hasAssignment ? assignedColor : unassignedColor;
        }
    }
    
    /// <summary>
    /// Update the assigned character display
    /// </summary>
    public void UpdateAssignment(string characterName)
    {
        bool hasAssignment = !string.IsNullOrEmpty(characterName);
        
        if (assignedCharacterText != null)
        {
            assignedCharacterText.text = hasAssignment ? $"Playing: {characterName}" : "No character assigned";
            assignedCharacterText.color = hasAssignment ? assignedColor : unassignedColor;
        }
        
        if (assignedIndicator != null)
        {
            assignedIndicator.SetActive(hasAssignment);
        }
        
        if (unassignedIndicator != null)
        {
            unassignedIndicator.SetActive(!hasAssignment);
        }
        
        if (statusColor != null)
        {
            statusColor.color = hasAssignment ? assignedColor : unassignedColor;
        }
    }
    
    /// <summary>
    /// Get the player data for this item
    /// </summary>
    public ConnectedPlayer GetPlayerData()
    {
        return playerData;
    }
}

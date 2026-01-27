using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// UI component for displaying a player character in the CampaignManager's CharactersPage.
/// Attach this to the CharacterItem prefab.
/// Now includes optional assignment functionality for the DM.
/// </summary>
public class PlayerCharacterItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI raceText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private Image characterImage;
    
    [Header("Assignment UI (Optional - for DM)")]
    [SerializeField] private TMP_Dropdown assignmentDropdown;
    [SerializeField] private TextMeshProUGUI assignedPlayerText;
    [SerializeField] private GameObject assignmentPanel; // Parent object to show/hide

    private string filePath;
    private CharacterData characterData;
    private List<(string playerId, string username)> playerOptions = new List<(string, string)>();
    private bool isInitialized = false;

    /// <summary>
    /// The file path to the character JSON file.
    /// </summary>
    public string FilePath => filePath;

    /// <summary>
    /// The loaded character data.
    /// </summary>
    public CharacterData Data => characterData;

    /// <summary>
    /// Initialize the item with character data.
    /// </summary>
    /// <param name="jsonFilePath">Path to the character JSON file</param>
    /// <param name="data">Parsed character data</param>
    public void Setup(string jsonFilePath, CharacterData data)
    {
        filePath = jsonFilePath;
        characterData = data;

        // Populate UI
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(data.charName) ? "(Unnamed)" : data.charName;
        }

        if (raceText != null)
        {
            raceText.text = !string.IsNullOrEmpty(data.race) ? data.race : "Unknown";
        }

        if (classText != null)
        {
            classText.text = !string.IsNullOrEmpty(data.charClass) ? data.charClass : "Unknown";
        }

        // Load token image if available
        if (characterImage != null && !string.IsNullOrEmpty(data.tokenFileName))
        {
            string folder = System.IO.Path.GetDirectoryName(jsonFilePath);
            string tokenPath = System.IO.Path.Combine(folder, data.tokenFileName);
            
            if (System.IO.File.Exists(tokenPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    characterImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    characterImage.preserveAspect = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"PlayerCharacterItem: Failed to load token image: {ex.Message}");
                }
            }
        }
        
        // Setup assignment UI if present
        SetupAssignmentUI();
        isInitialized = true;
    }
    
    /// <summary>
    /// Setup the assignment dropdown (only shown for DM/host)
    /// </summary>
    private void SetupAssignmentUI()
    {
        // Only show assignment UI if we're the host and the UI elements exist
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        
        Debug.Log($"PlayerCharacterItem.SetupAssignmentUI: isHost={isHost}, NetworkManager={NetworkManager.Singleton != null}");
        
        if (assignmentPanel != null)
        {
            assignmentPanel.SetActive(isHost);
        }
        
        if (!isHost) return;
        
        // Setup dropdown
        if (assignmentDropdown != null)
        {
            assignmentDropdown.onValueChanged.AddListener(OnAssignmentDropdownChanged);
            RefreshPlayerOptions();
        }
        
        // Show current assignment
        UpdateAssignmentDisplay();
        
        // Subscribe to player connection events
        SubscribeToConnectionManager();
    }
    
    private void SubscribeToConnectionManager()
    {
        if (PlayerConnectionManager.Instance != null)
        {
            Debug.Log($"PlayerCharacterItem: Subscribing to PlayerConnectionManager events");
            PlayerConnectionManager.Instance.OnPlayersListUpdated += RefreshPlayerOptions;
            PlayerConnectionManager.Instance.OnCharacterAssigned += OnCharacterAssignmentChanged;
            PlayerConnectionManager.Instance.OnCharacterUnassigned += OnCharacterUnassignmentChanged;
        }
        else
        {
            Debug.LogWarning($"PlayerCharacterItem: PlayerConnectionManager.Instance is null, will retry...");
            // Retry subscription after a short delay
            StartCoroutine(RetrySubscription());
        }
    }
    
    private System.Collections.IEnumerator RetrySubscription()
    {
        // Wait a bit for the manager to be ready
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.5f);
            
            if (PlayerConnectionManager.Instance != null)
            {
                Debug.Log($"PlayerCharacterItem: PlayerConnectionManager found on retry {i+1}");
                PlayerConnectionManager.Instance.OnPlayersListUpdated += RefreshPlayerOptions;
                PlayerConnectionManager.Instance.OnCharacterAssigned += OnCharacterAssignmentChanged;
                PlayerConnectionManager.Instance.OnCharacterUnassigned += OnCharacterUnassignmentChanged;
                RefreshPlayerOptions(); // Refresh now that we have the manager
                yield break;
            }
        }
        Debug.LogError("PlayerCharacterItem: PlayerConnectionManager.Instance never became available!");
    }
    
    void OnDestroy()
    {
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnPlayersListUpdated -= RefreshPlayerOptions;
            PlayerConnectionManager.Instance.OnCharacterAssigned -= OnCharacterAssignmentChanged;
            PlayerConnectionManager.Instance.OnCharacterUnassigned -= OnCharacterUnassignmentChanged;
        }
    }
    
    /// <summary>
    /// Refresh the dropdown with current connected players
    /// </summary>
    public void RefreshPlayerOptions()
    {
        if (assignmentDropdown == null) return;
        
        playerOptions.Clear();
        playerOptions.Add(("", "-- Unassigned --"));
        
        if (PlayerConnectionManager.Instance != null)
        {
            var players = PlayerConnectionManager.Instance.GetConnectedPlayers(false); // Don't include host
            Debug.Log($"PlayerCharacterItem.RefreshPlayerOptions: Found {players.Count} connected players");
            foreach (var player in players)
            {
                Debug.Log($"  - {player.username} ({player.playerId})");
                playerOptions.Add((player.playerId, player.username));
            }
        }
        else
        {
            Debug.LogWarning("PlayerCharacterItem.RefreshPlayerOptions: PlayerConnectionManager.Instance is null");
        }
        
        // Update dropdown options
        assignmentDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var p in playerOptions)
        {
            options.Add(new TMP_Dropdown.OptionData(p.username));
        }
        assignmentDropdown.AddOptions(options);
        
        // Set current selection
        UpdateAssignmentDisplay();
    }
    
    /// <summary>
    /// Update the display to show current assignment
    /// </summary>
    private void UpdateAssignmentDisplay()
    {
        if (characterData == null) return;
        
        var assignment = PlayerConnectionManager.Instance?.GetCharacterAssignment(characterData.id);
        
        if (assignedPlayerText != null)
        {
            assignedPlayerText.text = assignment != null ? assignment.assignedUsername : "Unassigned";
        }
        
        if (assignmentDropdown != null)
        {
            int selectedIndex = 0;
            if (assignment != null)
            {
                for (int i = 0; i < playerOptions.Count; i++)
                {
                    if (playerOptions[i].playerId == assignment.assignedPlayerId)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            assignmentDropdown.SetValueWithoutNotify(selectedIndex);
        }
    }
    
    /// <summary>
    /// Called when the dropdown selection changes
    /// </summary>
    private void OnAssignmentDropdownChanged(int index)
    {
        if (!isInitialized || characterData == null) return;
        if (index < 0 || index >= playerOptions.Count) return;
        
        var selected = playerOptions[index];
        
        if (string.IsNullOrEmpty(selected.playerId))
        {
            // Unassign
            PlayerConnectionManager.Instance?.UnassignCharacter(characterData.id);
        }
        else
        {
            // Assign
            PlayerConnectionManager.Instance?.AssignPlayerToCharacter(characterData.id, selected.playerId, selected.username);
        }
    }
    
    /// <summary>
    /// Called when any character is assigned
    /// </summary>
    private void OnCharacterAssignmentChanged(string characterId, string playerId, string username)
    {
        if (characterData != null && characterData.id == characterId)
        {
            UpdateAssignmentDisplay();
        }
    }
    
    /// <summary>
    /// Called when any character is unassigned
    /// </summary>
    private void OnCharacterUnassignmentChanged(string characterId)
    {
        if (characterData != null && characterData.id == characterId)
        {
            UpdateAssignmentDisplay();
        }
    }
}

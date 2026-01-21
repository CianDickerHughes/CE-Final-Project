using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loads and displays characters in the WaitingRoom scene.
/// Allows users to select a character before clicking Ready.
/// Works with CharacterReadySender to send the selected character to the host.
/// </summary>
public class WaitingRoomCharacterLoader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject itemPrefab; // PlayableCharacterItem prefab
    [SerializeField] private Transform contentParent; // Content transform inside ScrollView
    [SerializeField] private TextMeshProUGUI emptyText; // Shown when no characters exist

    [Header("Selection")]
    [SerializeField] private TextMeshProUGUI selectedCharacterText; // Optional: shows currently selected character name

    private PlayableCharacterItem selectedItem;

    /// <summary>
    /// The currently selected character item, or null if none selected.
    /// </summary>
    public PlayableCharacterItem SelectedItem => selectedItem;

    /// <summary>
    /// The file path of the selected character, or null if none selected.
    /// </summary>
    public string SelectedCharacterPath => selectedItem?.FilePath;

    /// <summary>
    /// Event raised when selection changes. Parameter is the new selected item (can be null).
    /// </summary>
    public event Action<PlayableCharacterItem> OnSelectionChanged;

    private void Start()
    {
        LoadCharacters();
    }

    /// <summary>
    /// Load all characters from the Characters folder and populate the UI.
    /// </summary>
    public void LoadCharacters()
    {
        // Clear existing items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        selectedItem = null;
        CharacterSelectionContext.Clear();

        // Get all character JSON files
        string[] files = CharacterIO.GetSavedCharacterFilePaths();
        
        // Debug logging
        string folder = CharacterIO.GetCharactersFolder();
        Debug.Log($"WaitingRoomCharacterLoader: Looking for characters in: {folder}");
        Debug.Log($"WaitingRoomCharacterLoader: Found {(files?.Length ?? 0)} character files");

        if (files == null || files.Length == 0)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(true);
                emptyText.text = "No characters found. You can continue without a character.";
            }
            UpdateSelectedText();
            OnSelectionChanged?.Invoke(null);
            return;
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }

        // Sort files by last modified date, newest first
        Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

        // Create UI items for each character
        foreach (string filePath in files)
        {
            try
            {
                Debug.Log($"WaitingRoomCharacterLoader: Loading character from: {filePath}");
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<CharacterData>(json);
                Debug.Log($"WaitingRoomCharacterLoader: Parsed character: {data?.charName ?? "null"}");

                // Optionally filter to only show characters owned by current user
                // Uncomment the following if you want this behavior:
                // if (!data.IsOwnedByCurrentUser()) continue;

                GameObject itemGO = Instantiate(itemPrefab, contentParent, false);
                var item = itemGO.GetComponent<PlayableCharacterItem>();

                if (item != null)
                {
                    item.Setup(filePath, data, OnItemSelected);
                    Debug.Log($"WaitingRoomCharacterLoader: Successfully created item for {data?.charName}");
                }
                else
                {
                    Debug.LogWarning("WaitingRoomCharacterLoader: PlayableCharacterItem component missing on prefab.");
                    Destroy(itemGO);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WaitingRoomCharacterLoader: Failed to load character from {filePath}: {ex.Message}");
            }
        }

        UpdateSelectedText();
    }

    /// <summary>
    /// Called when a character item is selected.
    /// </summary>
    private void OnItemSelected(PlayableCharacterItem item)
    {
        // Deselect previous item
        if (selectedItem != null && selectedItem != item)
        {
            selectedItem.SetSelected(false);
        }

        // Toggle selection if clicking the same item
        if (selectedItem == item)
        {
            selectedItem.SetSelected(false);
            selectedItem = null;
            CharacterSelectionContext.Clear();
        }
        else
        {
            // Select new item
            selectedItem = item;
            selectedItem.SetSelected(true);
            CharacterSelectionContext.SelectedCharacterFilePath = item.FilePath;
        }

        UpdateSelectedText();
        OnSelectionChanged?.Invoke(selectedItem);

        Debug.Log($"WaitingRoomCharacterLoader: Selected character: {(selectedItem != null ? selectedItem.Data.charName : "None")}");
    }

    /// <summary>
    /// Clear the current selection.
    /// </summary>
    public void ClearSelection()
    {
        if (selectedItem != null)
        {
            selectedItem.SetSelected(false);
            selectedItem = null;
        }
        CharacterSelectionContext.Clear();
        UpdateSelectedText();
        OnSelectionChanged?.Invoke(null);
    }

    /// <summary>
    /// Update the selected character text display.
    /// </summary>
    private void UpdateSelectedText()
    {
        if (selectedCharacterText != null)
        {
            if (selectedItem != null && selectedItem.Data != null)
            {
                selectedCharacterText.text = $"Selected: {selectedItem.Data.charName}";
            }
            else
            {
                selectedCharacterText.text = "No character selected";
            }
        }
    }

    /// <summary>
    /// Refresh the character list (useful after network sync).
    /// </summary>
    public void Refresh()
    {
        LoadCharacters();
    }
}

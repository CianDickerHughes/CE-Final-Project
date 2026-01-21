using System;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>
/// Loads and displays player characters from the campaign's PlayerCharacters folder.
/// Attach this to the CharactersPage in the CampaignManager scene.
/// </summary>
public class PlayerCharactersPageController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject characterItemPrefab; // CharacterItem prefab with PlayerCharacterItem script
    [SerializeField] private Transform contentParent; // Content transform inside ScrollView
    [SerializeField] private TextMeshProUGUI emptyText; // Shown when no characters exist
    [SerializeField] private TextMeshProUGUI titleText; // Optional title text

    [Header("Settings")]
    [SerializeField] private bool autoRefreshOnEnable = true;

    private void OnEnable()
    {
        if (autoRefreshOnEnable)
        {
            LoadPlayerCharacters();
        }
    }

    /// <summary>
    /// Load all player characters from the campaign's PlayerCharacters folder.
    /// </summary>
    public void LoadPlayerCharacters()
    {
        // Clear existing items
        if (contentParent != null)
        {
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Get the PlayerCharacters folder for the current campaign
        string playerCharactersFolder = GetPlayerCharactersFolder();

        if (string.IsNullOrEmpty(playerCharactersFolder) || !Directory.Exists(playerCharactersFolder))
        {
            ShowEmptyState("No player characters folder found.");
            Debug.Log($"PlayerCharactersPageController: Folder not found: {playerCharactersFolder}");
            return;
        }

        // Get all character JSON files
        string[] files = Directory.GetFiles(playerCharactersFolder, "*.json");

        if (files == null || files.Length == 0)
        {
            ShowEmptyState("No player characters yet.\nCharacters will appear here when players join and select their characters.");
            return;
        }

        // Hide empty text
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }

        // Sort files by last modified date, newest first
        Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

        int loadedCount = 0;

        // Create UI items for each character
        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<CharacterData>(json);

                if (data == null)
                {
                    Debug.LogWarning($"PlayerCharactersPageController: Failed to parse character from {filePath}");
                    continue;
                }

                GameObject itemGO = Instantiate(characterItemPrefab, contentParent, false);
                var item = itemGO.GetComponent<PlayerCharacterItem>();

                if (item != null)
                {
                    item.Setup(filePath, data);
                    loadedCount++;
                    Debug.Log($"PlayerCharactersPageController: Loaded character: {data.charName}");
                }
                else
                {
                    Debug.LogWarning("PlayerCharactersPageController: PlayerCharacterItem component missing on prefab.");
                    Destroy(itemGO);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerCharactersPageController: Failed to load character from {filePath}: {ex.Message}");
            }
        }

        // Update title if assigned
        if (titleText != null)
        {
            titleText.text = $"Player Characters ({loadedCount})";
        }

        Debug.Log($"PlayerCharactersPageController: Loaded {loadedCount} player characters from {playerCharactersFolder}");
    }

    /// <summary>
    /// Gets the PlayerCharacters folder for the current campaign.
    /// </summary>
    private string GetPlayerCharactersFolder()
    {
        // Try to get the campaign folder from CampaignSelectionContext
        if (CampaignSelectionContext.HasSelection && !string.IsNullOrEmpty(CampaignSelectionContext.SelectedCampaignFilePath))
        {
            string campaignFolder = Path.GetDirectoryName(CampaignSelectionContext.SelectedCampaignFilePath);
            string playerCharactersFolder = Path.Combine(campaignFolder, "PlayerCharacters");
            Debug.Log($"PlayerCharactersPageController: Using campaign folder from CampaignSelectionContext: {playerCharactersFolder}");
            return playerCharactersFolder;
        }

        // Try SceneDataTransfer as fallback
        if (SceneDataTransfer.Instance != null)
        {
            string campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            if (!string.IsNullOrEmpty(campaignId))
            {
                string campaignsFolder = CampaignManager.GetCampaignsFolder();
                
                if (Directory.Exists(campaignsFolder))
                {
                    foreach (string folder in Directory.GetDirectories(campaignsFolder))
                    {
                        string campaignJsonPath = Path.Combine(folder, $"{campaignId}.json");
                        if (File.Exists(campaignJsonPath))
                        {
                            string playerCharactersFolder = Path.Combine(folder, "PlayerCharacters");
                            Debug.Log($"PlayerCharactersPageController: Found campaign folder via SceneDataTransfer: {playerCharactersFolder}");
                            return playerCharactersFolder;
                        }
                    }
                }
            }
        }

        Debug.LogWarning("PlayerCharactersPageController: Could not determine campaign folder.");
        return null;
    }

    /// <summary>
    /// Show the empty state message.
    /// </summary>
    private void ShowEmptyState(string message)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = message;
        }
    }

    /// <summary>
    /// Manually refresh the character list.
    /// </summary>
    public void Refresh()
    {
        LoadPlayerCharacters();
    }

    /// <summary>
    /// Subscribe to character stored events from the network.
    /// Call this if you want auto-refresh when new characters arrive.
    /// </summary>
    public void SubscribeToNetworkEvents()
    {
        if (CharacterTransferNetwork.Instance != null)
        {
            CharacterTransferNetwork.Instance.OnCharacterStored += OnCharacterStored;
        }
    }

    /// <summary>
    /// Unsubscribe from network events.
    /// </summary>
    public void UnsubscribeFromNetworkEvents()
    {
        if (CharacterTransferNetwork.Instance != null)
        {
            CharacterTransferNetwork.Instance.OnCharacterStored -= OnCharacterStored;
        }
    }

    private void OnCharacterStored(string jsonFileName, string tokenFileName)
    {
        Debug.Log($"PlayerCharactersPageController: New character stored: {jsonFileName}");
        LoadPlayerCharacters();
    }

    private void OnDestroy()
    {
        UnsubscribeFromNetworkEvents();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Displays connected players in the PlayersPanel during gameplay.
/// Each player entry shows their username and a kick button (DM only).
/// Attach this to the PlayersPanel GameObject in GameplayScene.
/// </summary>
public class PlayersListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContainer;     // Parent for player items (e.g. a vertical layout group)
    [SerializeField] private GameObject playerItemPrefab;       // Prefab with ConnectedPlayerItemUI or fallback layout
    [SerializeField] private TextMeshProUGUI explanationText;   // The existing "Explanation" text to hide once populated

    void OnEnable()
    {
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnPlayerConnected += OnPlayerChanged;
            PlayerConnectionManager.Instance.OnPlayerDisconnected += OnPlayerDisconnected;
            PlayerConnectionManager.Instance.OnPlayersListUpdated += RefreshList;
        }

        RefreshList();
    }

    void OnDisable()
    {
        if (PlayerConnectionManager.Instance != null)
        {
            PlayerConnectionManager.Instance.OnPlayerConnected -= OnPlayerChanged;
            PlayerConnectionManager.Instance.OnPlayerDisconnected -= OnPlayerDisconnected;
            PlayerConnectionManager.Instance.OnPlayersListUpdated -= RefreshList;
        }
    }

    private void OnPlayerChanged(ConnectedPlayer _) => RefreshList();
    private void OnPlayerDisconnected(ulong _) => RefreshList();

    /// <summary>
    /// Rebuild the player list UI from current connection data.
    /// </summary>
    public void RefreshList()
    {
        if (playerListContainer == null) return;

        // Clear existing items
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        // Hide explanation text once we have players
        if (explanationText != null)
        {
            explanationText.gameObject.SetActive(false);
        }

        if (PlayerConnectionManager.Instance == null)
        {
            ShowExplanation("Waiting for network...");
            return;
        }

        // Include host so DM is visible in the list
        List<ConnectedPlayer> players = PlayerConnectionManager.Instance.GetConnectedPlayers(includeHost: true);

        if (players.Count == 0)
        {
            ShowExplanation("No players connected yet.");
            return;
        }

        // Get assignment data for display
        var campaign = CampaignManager.Instance?.GetCurrentCampaign();

        foreach (ConnectedPlayer player in players)
        {
            // Look up character assignment if available
            string assignedCharName = null;
            if (campaign?.characterAssignments != null)
            {
                var assignment = campaign.characterAssignments.GetAssignmentForPlayer(player.playerId);
                if (assignment != null)
                {
                    assignedCharName = GetCharacterName(assignment.characterId, campaign);
                }
            }

            if (playerItemPrefab != null)
            {
                GameObject item = Instantiate(playerItemPrefab, playerListContainer);
                var itemUI = item.GetComponent<ConnectedPlayerItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(player, assignedCharName);
                }
                else
                {
                    // Fallback: look for text + button in prefab
                    SetupFallbackItem(item, player);
                }
            }
            else
            {
                // No prefab assigned – create a simple row dynamically
                CreateDynamicPlayerRow(player);
            }
        }
    }

    /// <summary>
    /// Create a simple player row at runtime when no prefab is assigned.
    /// </summary>
    private void CreateDynamicPlayerRow(ConnectedPlayer player)
    {
        GameObject row = new GameObject(player.username, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(playerListContainer, false);

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 8;

        // Player name label
        GameObject nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(row.transform, false);
        var nameText = nameGO.GetComponent<TextMeshProUGUI>();
        nameText.text = player.isHost ? $"{player.username} (DM)" : player.username;
        nameText.fontSize = 18;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;

        // Kick button (host only, not for the host entry itself)
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isHost && !player.isHost)
        {
            GameObject btnGO = new GameObject("KickBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(row.transform, false);

            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(60, 30);
            var btnImage = btnGO.GetComponent<Image>();
            btnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
            btnText.text = "Kick";
            btnText.fontSize = 14;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;

            var button = btnGO.GetComponent<Button>();
            ulong clientId = player.clientId;
            button.onClick.AddListener(() =>
            {
                PlayerConnectionManager.Instance?.KickPlayer(clientId);
            });
        }
    }

    /// <summary>
    /// Fallback setup when prefab doesn't have ConnectedPlayerItemUI.
    /// </summary>
    private void SetupFallbackItem(GameObject item, ConnectedPlayer player)
    {
        var text = item.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = player.isHost ? $"{player.username} (DM)" : player.username;
        }

        // Try to find and wire up a kick button
        var kickBtn = item.GetComponentInChildren<Button>();
        if (kickBtn != null)
        {
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            if (isHost && !player.isHost)
            {
                kickBtn.gameObject.SetActive(true);
                ulong clientId = player.clientId;
                kickBtn.onClick.AddListener(() =>
                {
                    PlayerConnectionManager.Instance?.KickPlayer(clientId);
                });
            }
            else
            {
                kickBtn.gameObject.SetActive(false);
            }
        }
    }

    private void ShowExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.gameObject.SetActive(true);
            explanationText.text = message;
        }
    }

    private string GetCharacterName(string characterId, Campaign campaign)
    {
        if (campaign == null) return null;
        foreach (var pc in campaign.playerCharacters)
        {
            if (pc.characterData != null && pc.characterData.id == characterId)
                return pc.characterData.charName;
        }
        return null;
    }
}

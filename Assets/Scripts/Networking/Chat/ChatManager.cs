using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple chat manager - connects SendButton, InputMessage, Content, and message prefabs
/// Handles UI display. Uses ChatNetwork for networked messaging.
/// Now supports loading saved messages on scene change.
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("Connect These in Inspector")]
    [Tooltip("Your SendMSGBtn button")]
    [SerializeField] private Button sendButton;
    
    [Tooltip("Your InputMessage input field")]
    [SerializeField] private TMP_InputField inputField;
    
    [Tooltip("Your Content object (holds messages)")]
    [SerializeField] private Transform contentParent;
    
    [Tooltip("Your Messages scroll rect (optional)")]
    [SerializeField] private ScrollRect scrollRect;
    
    [Tooltip("Prefab with TextMeshProUGUI for displaying messages")]
    [SerializeField] private GameObject chatMessagePrefab;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 50;
    [SerializeField] private string playerName = "Player";
    
    [Header("Message Persistence")]
    [Tooltip("Load saved messages when the scene starts")]
    [SerializeField] private bool loadSavedMessagesOnStart = true;

    private List<GameObject> messageObjects = new List<GameObject>();
    private HashSet<string> displayedMessageIds = new HashSet<string>(); // Track displayed messages to avoid duplicates

    private void Start()
    {
        // Set player name from logged-in user
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.CurrentUsername))
        {
            playerName = SessionManager.Instance.CurrentUsername;
        }

        // Connect send button
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessage);
        }

        // Allow Enter key to send
        if (inputField != null)
        {
            inputField.onSubmit.AddListener((text) => SendMessage());
        }

        // Try to find ChatNetwork - will retry in Update if not found yet
        TryConnectToChatNetwork();
        
        // Load saved messages if enabled
        if (loadSavedMessagesOnStart)
        {
            LoadSavedMessages();
        }
    }

    private void Update()
    {
        // Keep trying to connect if not connected yet
        if (ChatNetwork.Instance != null && !isConnectedToNetwork)
        {
            TryConnectToChatNetwork();
        }
    }

    private bool isConnectedToNetwork = false;

    private void TryConnectToChatNetwork()
    {
        if (ChatNetwork.Instance != null && !isConnectedToNetwork)
        {
            ChatNetwork.Instance.OnMessageReceived += DisplayMessage;
            ChatNetwork.Instance.OnChatHistoryLoaded += OnChatHistoryLoaded;
            isConnectedToNetwork = true;
            Debug.Log("ChatManager: Connected to ChatNetwork");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from network messages
        if (ChatNetwork.Instance != null)
        {
            ChatNetwork.Instance.OnMessageReceived -= DisplayMessage;
            ChatNetwork.Instance.OnChatHistoryLoaded -= OnChatHistoryLoaded;
        }
    }
    
    /// <summary>
    /// Load saved messages from ChatMessageStore and display them
    /// </summary>
    private void LoadSavedMessages()
    {
        // Try to initialize storage if not already done
        if (!ChatMessageStore.IsInitialized())
        {
            if (CampaignManager.Instance != null)
            {
                string campaignFolder = CampaignManager.Instance.GetCurrentCampaignFolder();
                if (!string.IsNullOrEmpty(campaignFolder))
                {
                    ChatMessageStore.Initialize(campaignFolder);
                }
            }
        }
        
        // Get and display saved messages
        List<ChatMessage> savedMessages = ChatMessageStore.GetMessages();
        Debug.Log($"ChatManager: Loading {savedMessages.Count} saved messages");
        
        foreach (var msg in savedMessages)
        {
            DisplayMessageInternal(msg.senderId, msg.senderName, msg.message, msg.timestamp);
        }
    }
    
    /// <summary>
    /// Called when chat history is loaded from server
    /// </summary>
    private void OnChatHistoryLoaded(List<ChatMessage> messages)
    {
        Debug.Log($"ChatManager: Chat history loaded with {messages.Count} messages");
        // Messages are already displayed via OnMessageReceived events
    }

    /// <summary>
    /// Sends message when button clicked or Enter pressed
    /// </summary>
    private void SendMessage()
    {
        // Check if message is empty
        if (string.IsNullOrWhiteSpace(inputField.text))
        {
            return;
        }

        // Check if network is available
        if (ChatNetwork.Instance == null)
        {
            Debug.LogWarning("ChatManager: ChatNetwork not available. Cannot send message.");
            return;
        }

        string message = inputField.text.Trim();
        
        // Send to all players via network
        ChatNetwork.Instance.SendMessage(message, playerName);
        
        // Clear input
        inputField.text = "";
        inputField.ActivateInputField();
    }

    /// <summary>
    /// Displays message in the Content object
    /// </summary>
    private void DisplayMessage(ulong senderId, string senderName, string message)
    {
        // Generate a simple ID for deduplication (new messages won't have timestamp yet)
        DisplayMessageInternal(senderId, senderName, message, null);
    }
    
    /// <summary>
    /// Internal method to display a message with optional timestamp for deduplication
    /// </summary>
    private void DisplayMessageInternal(ulong senderId, string senderName, string message, string timestamp)
    {
        if (chatMessagePrefab == null || contentParent == null)
        {
            Debug.LogWarning("ChatManager: Assign Chat Message Prefab and Content in inspector!");
            return;
        }
        
        // Create a unique ID for this message to prevent duplicates
        string messageId = $"{senderId}_{senderName}_{message}_{timestamp ?? "new"}";
        
        // Check for duplicates (skip if already displayed)
        if (displayedMessageIds.Contains(messageId))
        {
            return;
        }
        displayedMessageIds.Add(messageId);

        // Create message from prefab in Content
        GameObject messageObj = Instantiate(chatMessagePrefab, contentParent);
        
        // Try to find TextMeshProUGUI and set the text
        TextMeshProUGUI textComponent = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = $"{senderName}: {message}";
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 6f;
            textComponent.fontSizeMax = 10f;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Truncate;
        }

        // Add to list
        messageObjects.Add(messageObj);

        // Remove old messages if too many
        if (messageObjects.Count > maxMessages)
        {
            GameObject oldMessage = messageObjects[0];
            messageObjects.RemoveAt(0);
            Destroy(oldMessage);
        }

        // Scroll to bottom (ensure it happens after layout updates)
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    /// <summary>
    /// Set the player name (call this from your character/authentication system)
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            playerName = name;
        }
    }
    
    /// <summary>
    /// Clear all displayed messages from the UI
    /// </summary>
    public void ClearDisplayedMessages()
    {
        foreach (var messageObj in messageObjects)
        {
            if (messageObj != null)
            {
                Destroy(messageObj);
            }
        }
        messageObjects.Clear();
        displayedMessageIds.Clear();
    }
    
    /// <summary>
    /// Refresh the chat by clearing and reloading saved messages
    /// </summary>
    public void RefreshChat()
    {
        ClearDisplayedMessages();
        LoadSavedMessages();
    }

    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // wait one frame so layout can rebuild
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

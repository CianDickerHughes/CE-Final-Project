using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple chat manager - connects SendButton, InputMessage, Content, and message prefabs
/// Handles UI display. Uses ChatNetwork for networked messaging.
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

    private List<GameObject> messageObjects = new List<GameObject>();

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
        }
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
        if (chatMessagePrefab == null || contentParent == null)
        {
            Debug.LogWarning("ChatManager: Assign Chat Message Prefab and Content in inspector!");
            return;
        }

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
            textComponent.enableWordWrapping = true;
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Simple chat manager - connects SendButton, InputMessage, Content, and message prefabs
/// Handles networked messaging across all players
/// </summary>
public class ChatManager : NetworkBehaviour
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

        // Check if network is running and connected
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogWarning("ChatManager: Network not connected. Cannot send message.");
            return;
        }

        string message = inputField.text.Trim();
        
        // Send to all players via network
        SendMessageServerRpc(message, playerName);
        
        // Clear input
        inputField.text = "";
        inputField.ActivateInputField();
    }

    /// <summary>
    /// Sends message to server
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendMessageServerRpc(string message, string senderName, RpcParams rpcParams = default)
    {
        // Get sender ID
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        // Broadcast to all clients
        ReceiveMessageClientRpc(senderId, senderName, message);
    }

    /// <summary>
    /// Receives message and displays it
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveMessageClientRpc(ulong senderId, string senderName, string message)
    {
        DisplayMessage(senderId, senderName, message);
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

        // Scroll to bottom
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
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
}

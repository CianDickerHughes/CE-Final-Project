using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles networked chat messaging via RPCs.
/// This is spawned as a network prefab and exists across all scenes.
/// UI ChatManagers find this instance to send/receive messages.
/// Now also handles message persistence via ChatMessageStore.
/// </summary>
public class ChatNetwork : NetworkBehaviour
{
    public static ChatNetwork Instance { get; private set; }

    // Event that UI ChatManagers subscribe to
    public event Action<ulong, string, string> OnMessageReceived;
    
    // Event fired when chat history is loaded (for UI to display saved messages)
    public event Action<List<ChatMessage>> OnChatHistoryLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Initialize ChatMessageStore with campaign folder
        InitializeChatStorage();
        
        // If we're a client that just joined, request chat history from server
        if (!IsServer && IsClient)
        {
            RequestChatHistoryServerRpc();
        }
    }

    private void InitializeChatStorage()
    {
        // Get campaign folder from CampaignManager
        if (CampaignManager.Instance != null)
        {
            string campaignFolder = CampaignManager.Instance.GetCurrentCampaignFolder();
            if (!string.IsNullOrEmpty(campaignFolder))
            {
                ChatMessageStore.Initialize(campaignFolder);
                Debug.Log($"ChatNetwork: Initialized chat storage for campaign");
            }
            else
            {
                Debug.LogWarning("ChatNetwork: No current campaign folder available");
            }
        }
        else
        {
            Debug.LogWarning("ChatNetwork: CampaignManager not available for chat storage");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Send a message from a client to the server
    /// </summary>
    public void SendMessage(string message, string senderName)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("ChatNetwork: Not spawned yet, cannot send message.");
            return;
        }

        SendMessageServerRpc(message, senderName);
    }

    /// <summary>
    /// Sends message to server
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SendMessageServerRpc(string message, string senderName, RpcParams rpcParams = default)
    {
        // Get sender ID
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        // Store the message on server (persists to disk)
        ChatMessageStore.AddMessage(senderId, senderName, message);
        
        // Broadcast to all clients
        ReceiveMessageClientRpc(senderId, senderName, message);
    }

    /// <summary>
    /// Receives message and broadcasts to listeners
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveMessageClientRpc(ulong senderId, string senderName, string message)
    {
        // Store locally on clients as well (for scene change persistence)
        if (!IsServer)
        {
            ChatMessageStore.AddMessage(senderId, senderName, message);
        }
        
        OnMessageReceived?.Invoke(senderId, senderName, message);
    }

    /// <summary>
    /// Request chat history from server (called by new clients)
    /// </summary>
    [Rpc(SendTo.Server)]
    private void RequestChatHistoryServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Get all stored messages
        List<ChatMessage> messages = ChatMessageStore.GetMessages();
        
        // Create params to target the specific client
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        
        // Send each message to the requesting client
        foreach (var msg in messages)
        {
            SendHistoryMessageClientRpc(msg.senderId, msg.senderName, msg.message, clientRpcParams);
        }
        
        // Signal end of history
        HistoryCompleteClientRpc(clientRpcParams);
        
        Debug.Log($"ChatNetwork: Sent {messages.Count} history messages to client {clientId}");
    }

    /// <summary>
    /// Receive a historical message (sent to specific client)
    /// </summary>
    [ClientRpc]
    private void SendHistoryMessageClientRpc(ulong senderId, string senderName, string message, ClientRpcParams clientRpcParams = default)
    {
        // Store in local cache
        ChatMessageStore.AddMessage(senderId, senderName, message);
        
        // Display in UI
        OnMessageReceived?.Invoke(senderId, senderName, message);
    }

    /// <summary>
    /// Signal that history sync is complete
    /// </summary>
    [ClientRpc]
    private void HistoryCompleteClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("ChatNetwork: Chat history sync complete");
        OnChatHistoryLoaded?.Invoke(ChatMessageStore.GetMessages());
    }

    /// <summary>
    /// Load and display saved messages (call when scene changes)
    /// </summary>
    public void LoadSavedMessages()
    {
        List<ChatMessage> messages = ChatMessageStore.GetMessages();
        OnChatHistoryLoaded?.Invoke(messages);
        Debug.Log($"ChatNetwork: Loaded {messages.Count} saved messages");
    }

    /// <summary>
    /// Get all stored messages
    /// </summary>
    public List<ChatMessage> GetStoredMessages()
    {
        return ChatMessageStore.GetMessages();
    }

    /// <summary>
    /// Re-initialize storage (call when campaign changes)
    /// </summary>
    public void ReinitializeStorage()
    {
        InitializeChatStorage();
    }
}

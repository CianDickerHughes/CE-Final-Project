using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Handles networked chat messaging via RPCs.
/// This is spawned as a network prefab and exists across all scenes.
/// UI ChatManagers find this instance to send/receive messages.
/// </summary>
public class ChatNetwork : NetworkBehaviour
{
    public static ChatNetwork Instance { get; private set; }

    // Event that UI ChatManagers subscribe to
    public event Action<ulong, string, string> OnMessageReceived;

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
        
        // Broadcast to all clients
        ReceiveMessageClientRpc(senderId, senderName, message);
    }

    /// <summary>
    /// Receives message and broadcasts to listeners
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveMessageClientRpc(ulong senderId, string senderName, string message)
    {
        OnMessageReceived?.Invoke(senderId, senderName, message);
    }
}

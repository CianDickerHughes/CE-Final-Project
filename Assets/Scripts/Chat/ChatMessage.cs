using System;
using Unity.Netcode;

/// <summary>
/// Represents a chat message structure for network transmission
/// </summary>
[Serializable]
public struct ChatMessage : INetworkSerializable
{
    public ulong senderId;
    public string senderName;
    public string messageText;
    public long timestamp;

    public ChatMessage(ulong senderId, string senderName, string messageText)
    {
        this.senderId = senderId;
        this.senderName = senderName;
        this.messageText = messageText;
        this.timestamp = DateTime.Now.Ticks;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref senderId);
        serializer.SerializeValue(ref senderName);
        serializer.SerializeValue(ref messageText);
        serializer.SerializeValue(ref timestamp);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Data structure for a single chat message
/// </summary>
[System.Serializable]
public class ChatMessage
{
    public ulong senderId;
    public string senderName;
    public string message;
    public string timestamp;

    public ChatMessage(ulong senderId, string senderName, string message)
    {
        this.senderId = senderId;
        this.senderName = senderName;
        this.message = message;
        this.timestamp = DateTime.Now.ToString("o");
    }
}

/// <summary>
/// Container for serializing chat messages to JSON
/// </summary>
[System.Serializable]
public class ChatMessageContainer
{
    public List<ChatMessage> messages = new List<ChatMessage>();
}

/// <summary>
/// Handles saving and loading chat messages to/from the campaign's Messages folder.
/// Messages are persisted across scene changes and sessions.
/// </summary>
public static class ChatMessageStore
{
    private const string MESSAGES_FOLDER = "Messages";
    private const string MESSAGES_FILE = "chat_history.json";
    private const int MAX_STORED_MESSAGES = 100;

    // In-memory message history (survives scene changes via static)
    private static List<ChatMessage> currentMessages = new List<ChatMessage>();
    private static string currentCampaignPath = null;

    /// <summary>
    /// Initialize the store for a specific campaign
    /// </summary>
    public static void Initialize(string campaignFolderPath)
    {
        if (string.IsNullOrEmpty(campaignFolderPath))
        {
            Debug.LogWarning("ChatMessageStore: Cannot initialize with null campaign path");
            return;
        }

        // If switching to a different campaign, save current messages first
        if (currentCampaignPath != null && currentCampaignPath != campaignFolderPath)
        {
            SaveMessages();
        }

        currentCampaignPath = campaignFolderPath;
        LoadMessages();
        Debug.Log($"ChatMessageStore: Initialized for campaign at {campaignFolderPath}");
    }

    /// <summary>
    /// Add a new message and save to disk
    /// </summary>
    public static void AddMessage(ulong senderId, string senderName, string message)
    {
        ChatMessage chatMessage = new ChatMessage(senderId, senderName, message);
        currentMessages.Add(chatMessage);

        // Trim old messages if exceeding max
        while (currentMessages.Count > MAX_STORED_MESSAGES)
        {
            currentMessages.RemoveAt(0);
        }

        // Auto-save after each message
        SaveMessages();
    }

    /// <summary>
    /// Get all stored messages
    /// </summary>
    public static List<ChatMessage> GetMessages()
    {
        return new List<ChatMessage>(currentMessages);
    }

    /// <summary>
    /// Get the number of stored messages
    /// </summary>
    public static int GetMessageCount()
    {
        return currentMessages.Count;
    }

    /// <summary>
    /// Clear all messages (useful for starting a new session)
    /// </summary>
    public static void ClearMessages()
    {
        currentMessages.Clear();
        SaveMessages();
    }

    /// <summary>
    /// Save messages to the campaign's Messages folder
    /// </summary>
    public static void SaveMessages()
    {
        if (string.IsNullOrEmpty(currentCampaignPath))
        {
            Debug.LogWarning("ChatMessageStore: No campaign path set, cannot save messages");
            return;
        }

        try
        {
            string messagesFolder = Path.Combine(currentCampaignPath, MESSAGES_FOLDER);
            if (!Directory.Exists(messagesFolder))
            {
                Directory.CreateDirectory(messagesFolder);
            }

            string filePath = Path.Combine(messagesFolder, MESSAGES_FILE);
            ChatMessageContainer container = new ChatMessageContainer
            {
                messages = currentMessages
            };

            string json = JsonUtility.ToJson(container, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"ChatMessageStore: Saved {currentMessages.Count} messages to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatMessageStore: Failed to save messages - {e.Message}");
        }
    }

    /// <summary>
    /// Load messages from the campaign's Messages folder
    /// </summary>
    public static void LoadMessages()
    {
        if (string.IsNullOrEmpty(currentCampaignPath))
        {
            Debug.LogWarning("ChatMessageStore: No campaign path set, cannot load messages");
            return;
        }

        try
        {
            string messagesFolder = Path.Combine(currentCampaignPath, MESSAGES_FOLDER);
            string filePath = Path.Combine(messagesFolder, MESSAGES_FILE);

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                ChatMessageContainer container = JsonUtility.FromJson<ChatMessageContainer>(json);
                
                if (container != null && container.messages != null)
                {
                    currentMessages = container.messages;
                    Debug.Log($"ChatMessageStore: Loaded {currentMessages.Count} messages from {filePath}");
                }
                else
                {
                    currentMessages = new List<ChatMessage>();
                }
            }
            else
            {
                currentMessages = new List<ChatMessage>();
                Debug.Log("ChatMessageStore: No existing messages file, starting fresh");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatMessageStore: Failed to load messages - {e.Message}");
            currentMessages = new List<ChatMessage>();
        }
    }

    /// <summary>
    /// Get the current campaign path
    /// </summary>
    public static string GetCurrentCampaignPath()
    {
        return currentCampaignPath;
    }

    /// <summary>
    /// Check if the store is initialized for a campaign
    /// </summary>
    public static bool IsInitialized()
    {
        return !string.IsNullOrEmpty(currentCampaignPath);
    }

    /// <summary>
    /// Get messages folder path for current campaign
    /// </summary>
    public static string GetMessagesFolderPath()
    {
        if (string.IsNullOrEmpty(currentCampaignPath))
            return null;
        
        return Path.Combine(currentCampaignPath, MESSAGES_FOLDER);
    }
}

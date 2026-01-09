using UnityEngine;
using TMPro;

/// <summary>
/// UI component for displaying a single chat message
/// Attach this to your chat message prefab
/// </summary>
public class ChatMessageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI senderNameText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI timestampText;

    /// <summary>
    /// Initialize the chat message UI with data
    /// </summary>
    public void SetMessage(string senderName, string message, long timestamp = 0)
    {
        if (senderNameText != null)
        {
            senderNameText.text = senderName + ":";
        }

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (timestampText != null && timestamp > 0)
        {
            System.DateTime time = new System.DateTime(timestamp);
            timestampText.text = time.ToString("HH:mm");
        }
    }

    /// <summary>
    /// Set message with color coding for different players
    /// </summary>
    public void SetMessage(string senderName, string message, Color nameColor, long timestamp = 0)
    {
        if (senderNameText != null)
        {
            senderNameText.text = senderName + ":";
            senderNameText.color = nameColor;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (timestampText != null && timestamp > 0)
        {
            System.DateTime time = new System.DateTime(timestamp);
            timestampText.text = time.ToString("HH:mm");
        }
    }
}

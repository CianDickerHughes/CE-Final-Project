using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows a "You have been kicked" popup on the Campaigns scene.
// Lives on a panel that starts disabled. A static flag triggers it on scene load.
public class KickNotification : MonoBehaviour
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    // Static flag set before loading the Campaigns scene
    private static bool wasKicked = false;
    private static string kickMessage = "";

    public static void SetKicked(string message = "You have been kicked from the session.")
    {
        wasKicked = true;
        kickMessage = message;
    }

    void Start()
    {
        closeButton.onClick.AddListener(CloseNotification);

        if (wasKicked)
        {
            messageText.text = kickMessage;
            notificationPanel.SetActive(true);
            wasKicked = false;
            kickMessage = "";
        }
        else
        {
            notificationPanel.SetActive(false);
        }
    }

    private void CloseNotification()
    {
        notificationPanel.SetActive(false);
    }
}

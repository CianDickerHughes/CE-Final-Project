using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class NetworkShutdownButton : MonoBehaviour
{
    [SerializeField] string backSceneName = "";

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnBackClicked);
        }
    }

    void OnBackClicked()
    {
        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"NetworkShutdownButton: Exception during shutdown: {ex}");
        }

        if (!string.IsNullOrEmpty(backSceneName))
        {
            SceneManager.LoadScene(backSceneName);
        }
    }
}
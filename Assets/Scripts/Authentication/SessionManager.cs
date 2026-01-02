using UnityEngine;

//Simple singleton that stores the current user's name
//Acts as a "global variable" for the current user

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    private const string PlayerPrefsKey = "CurrentUsername";
    //The current logged-in user's name
    public string CurrentUsername { get; private set; }

    //Check if a user is logged in
    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Load from PlayerPrefs if available
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                CurrentUsername = PlayerPrefs.GetString(PlayerPrefsKey);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //Set the current user (called when they click a user or create new one)
    public void SetCurrentUser(string username)
    {
        CurrentUsername = username.Trim();
        PlayerPrefs.SetString(PlayerPrefsKey, CurrentUsername);
        PlayerPrefs.Save();
        Debug.Log($"User set: {CurrentUsername}");
    }

    //Clear the current user
    public void SignOut()
    {
        CurrentUsername = null;
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("User signed out");
    }
}
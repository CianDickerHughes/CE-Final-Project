using UnityEngine;

//Simple singleton that stores the current user's name
//Acts as a "global variable" for the current user

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    private const string PlayerPrefsKey = "CurrentUsername";
    //The current logged-in user's name
    public string CurrentUsername { get; private set; }
    
    //The character selected for the current game session
    public CharacterData SelectedCharacter { get; private set; }

    //Check if a user is logged in
    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);
    
    //Check if a character is selected
    public bool HasSelectedCharacter => SelectedCharacter != null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Don't load from PlayerPrefs - fresh login required each app launch
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
    }
    
    //Set the selected character for gameplay
    public void SetSelectedCharacter(CharacterData character)
    {
        SelectedCharacter = character;
        Debug.Log($"Character selected for session: {character?.charName ?? "None"}");
    }
    
    //Clear the selected character
    public void ClearSelectedCharacter()
    {
        SelectedCharacter = null;
    }

    //Clear the current user
    public void SignOut()
    {
        CurrentUsername = null;
        SelectedCharacter = null;
    }
}
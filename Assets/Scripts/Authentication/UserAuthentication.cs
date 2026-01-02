using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles the user selection panel on the main menu
// Shows on app start, lets user select existing user or create new one
public class UserAuthentication : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    
    [Header("User List")]
    [SerializeField] private Transform userListContainer;
    [SerializeField] private GameObject userItemPrefab;
    
    [Header("Create New User")]
    [SerializeField] private TMP_InputField newUsernameInput;
    [SerializeField] private Button createUserButton;
    
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    void Start()
    {
        //If already signed in, hide panel immediately
        //This can happen if returning from another scene
        //This is where things can get a bit tricky - in edit mode the SessionManager might persist which is useful for testing but prevents this from popping up again
        //Not a problem just something to be aware of
        if (SessionManager.Instance != null && SessionManager.Instance.IsLoggedIn)
        {
            panel.SetActive(false);
            return;
        }
        //Show the panel on start
        panel.SetActive(true);
        //Load and display existing users
        LoadUsers();
        //Set up create button
        createUserButton.onClick.AddListener(OnCreateUserClicked);
    }
    
    //Load all users and populate the list
    private void LoadUsers()
    {
        //Clear existing items
        foreach (Transform child in userListContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Get all saved users
        List<string> users = UserDataManager.Instance.GetAllUsers();
        
        if (users.Count == 0)
        {
            statusText.text = "No users found. Create one below.";
            return;
        }
        
        statusText.text = "Select a user to continue:";
        
        // Create a button for each user
        foreach (string username in users)
        {
            GameObject itemObj = Instantiate(userItemPrefab, userListContainer);
            
            // Set the username text
            TextMeshProUGUI usernameText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (usernameText != null)
            {
                usernameText.text = username;
            }
            
            // Set up the button to select this user
            Button button = itemObj.GetComponent<Button>();
            if (button != null)
            {
                string currentUser = username; // Capture for closure
                button.onClick.AddListener(() => SelectUser(currentUser));
            }
        }
    }
    
    // When user clicks on an existing user
    private void SelectUser(string username)
    {
        // Set this as the current user
        SessionManager.Instance.SetCurrentUser(username);
        
        // Close the panel
        panel.SetActive(false);
        
        Debug.Log($"Playing as: {username}");
    }
    
    // When user clicks "Create User" button
    private void OnCreateUserClicked()
    {
        string newUsername = newUsernameInput.text.Trim();
        
        if (string.IsNullOrEmpty(newUsername))
        {
            statusText.text = "Please enter a username";
            return;
        }
        
        // Try to create the user
        bool success = UserDataManager.Instance.AddUser(newUsername);
        
        if (success)
        {
            // Set as current user
            SessionManager.Instance.SetCurrentUser(newUsername);
            // Close the panel
            panel.SetActive(false);
            Debug.Log($"Created and playing as: {newUsername}");
        }
        else
        {
            statusText.text = "Username already exists. Select it above or choose a different name.";
            // Reload user list in case another user was added externally
            LoadUsers();
        }
    }
}
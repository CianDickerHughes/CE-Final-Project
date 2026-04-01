using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Handles loading and saving user names to a JSON file in Assets/UserData
//Provides methods to get all users and add new users
//This file will potentially be removed later but for now it helps with user management
public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }
    private string userDataPath;
    private const string USER_FILE = "users.json";
    private List<string> users = new List<string>();

    //We load in all the users on Awake
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            userDataPath = Path.Combine(Application.dataPath, "UserData");
            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
            }
            LoadUsersFromFile();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<string> GetAllUsers()
    {
        return new List<string>(users);
    }

    //Adds a new user if it doesn't already exist
    //This just works by accepting a username string and adding it to the list
    public bool AddUser(string username)
    {
        username = username.Trim();
        //First we check to see if it already exists or is invalid
        if (string.IsNullOrEmpty(username) || users.Contains(username))
        {
            Debug.LogWarning("Invalid or duplicate username");
            return false;
        }
        //Otherwise add and save
        users.Add(username);
        SaveUsersToFile();
        return true;
    }

    //Method to save users to JSON file
    private void SaveUsersToFile()
    {
        string filePath = Path.Combine(userDataPath, USER_FILE);
        string json = JsonUtility.ToJson(new UserListWrapper { users = users }, true);
        File.WriteAllText(filePath, json);
    }

    //Method to load users from JSON file
    private void LoadUsersFromFile()
    {
        string filePath = Path.Combine(userDataPath, USER_FILE);
        if (!File.Exists(filePath))
        {
            users = new List<string>();
            return;
        }
        string json = File.ReadAllText(filePath);
        UserListWrapper wrapper = JsonUtility.FromJson<UserListWrapper>(json);
        users = wrapper != null && wrapper.users != null ? wrapper.users : new List<string>();
    }

    [System.Serializable]
    private class UserListWrapper
    {
        public List<string> users;
    }
}

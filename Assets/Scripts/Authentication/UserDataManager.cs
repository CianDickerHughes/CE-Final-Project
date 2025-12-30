using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Handles loading and saving user names to a JSON file in Assets/UserData
public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }
    private string userDataPath;
    private const string USER_FILE = "users.json";
    private List<string> users = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            userDataPath = Path.Combine(Application.dataPath, "UserData");
            if (!Directory.Exists(userDataPath))
                Directory.CreateDirectory(userDataPath);
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

    public bool AddUser(string username)
    {
        username = username.Trim();
        if (string.IsNullOrEmpty(username) || users.Contains(username))
            return false;
        users.Add(username);
        SaveUsersToFile();
        return true;
    }

    private void SaveUsersToFile()
    {
        string filePath = Path.Combine(userDataPath, USER_FILE);
        string json = JsonUtility.ToJson(new UserListWrapper { users = users }, true);
        File.WriteAllText(filePath, json);
    }

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

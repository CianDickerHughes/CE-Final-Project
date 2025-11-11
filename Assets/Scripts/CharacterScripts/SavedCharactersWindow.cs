using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SavedCharactersWindow : MonoBehaviour
{
    [Header("UI refs")]
    public GameObject itemPrefab; // SavedCharacterItem prefab
    public Transform contentParent; // Content transform inside ScrollView
    public Button closeButton;
    public Text titleText;

    //Callback set by CharacterCreatorUI - invoked when user selects a file to load
    private Action<string> onCharacterSelected;

    //We just add a listener to the close button
    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    //Call to open and populate the list
    //When a character is chosen we load it via the callback
    public void Open(Action<string> onSelected)
    {
        onCharacterSelected = onSelected;
        gameObject.SetActive(true);
        PopulateList();
    }

    //This is just to close the window and clean up
    public void Close()
    {
        // cleanup items to free memory
        foreach (Transform t in contentParent)
        {
            Destroy(t.gameObject);
        }
        gameObject.SetActive(false);
        onCharacterSelected = null;
    }

    void PopulateList()
    {
        // Clear existing entries (defensive)
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        string[] files = CharacterIO.GetSavedCharacterFilePaths();
        if (files == null || files.Length == 0)
        {
            // Optionally show "no saved characters" text inside the panel
            return;
        }

        Array.Sort(files); // optional: sort by name (timestamp suffix will order chronologically if you named accordingly)
        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<CharacterData>(json);

                GameObject go = Instantiate(itemPrefab, contentParent, false);
                var item = go.GetComponent<SavedCharacterItem>();
                if (item != null)
                {
                    item.Setup(filePath, data, OnItemSelected);
                }
                else
                {
                    Debug.LogWarning("SavedCharacterItem component missing on prefab.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to populate saved list entry: " + ex.Message);
            }
        }
    }

    void OnItemSelected(string jsonFilePath)
    {
        onCharacterSelected?.Invoke(jsonFilePath);
        Close();
    }
}

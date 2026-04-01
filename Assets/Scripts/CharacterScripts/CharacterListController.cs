using System;
using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterListController : MonoBehaviour
{
    public GameObject itemPrefab; // SavedCharacterItem prefab
    public Transform contentParent; // Content under ScrollView
    public Button createNewButton;
    public TextMeshProUGUI emptyText; // optional, show when no characters

    //When the page starts we populate the list of saved characters
    void Start()
    {
        if (createNewButton != null){
            createNewButton.onClick.AddListener(OnCreateNewClicked);
        }
        PopulateList();
    }

    //This is the main method for the page - it populates the list of saved characters for this specific user
    void PopulateList()
    {
        //Clear existing entries
        foreach (Transform t in contentParent){
            Destroy(t.gameObject);
        }

        //Using the character IO method to get all saved character file paths
        string[] files = CharacterIO.GetSavedCharacterFilePaths();
        if (files == null || files.Length == 0)
        {
            if (emptyText != null) emptyText.gameObject.SetActive(true);
            return;
        }

        //If we have files to show, hide the empty text
        if (emptyText != null) {
            emptyText.gameObject.SetActive(false);
        }

        //Sort files by last modified date, newest first - can remove this if we want later
        Array.Sort(files, (a,b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

        //Create an entry for each file and setup its data so we can represent it in the list/scrollview
        foreach (string filePath in files)
        {
            try
            {
                //Read the JSON file and parse it into CharacterData
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<CharacterData>(json);

                //Instantiate the item prefab and set it up
                GameObject go = Instantiate(itemPrefab, contentParent, false);
                var item = go.GetComponent<SavedCharacterItem>();
                if (item != null)
                {
                    // Pass an onSelect callback that opens Edit scene, and onDelete to refresh the list
                    item.Setup(filePath, data, OnItemSelected, OnCharacterDeleted);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to create list item: " + ex.Message);
            }
        }
    }

    //This is a method that moves the scene to create character
    void OnCreateNewClicked()
    {
        // Clear selection and load CreateCharacter scene
        CharacterSelectionContext.Clear();
        SceneManager.LoadScene("CreateCharacter");
    }

    //This is a simple method that gets the file path of the selected character JSON
    //It them moves us to the EditCharacter scene
    void OnItemSelected(string jsonFilePath)
    {
        //Set selection and load EditCharacter scene
        CharacterSelectionContext.SelectedCharacterFilePath = jsonFilePath;
        SceneManager.LoadScene("EditCharacter");
    }

    //This is called when a character is deleted to refresh the list
    void OnCharacterDeleted()
    {
        PopulateList();
    }
}

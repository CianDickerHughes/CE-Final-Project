using UnityEngine;
using System;
using TMPro;
using System.IO;
using UnityEngine.UI;

public class EnemyListController : MonoBehaviour
{
    //Variables for UI references
    public GameObject itemPrefab; // EnemyItem prefab
    public Transform contentParent; // Content under ScrollView
    public TextMeshProUGUI emptyText; // optional, show when no enemies
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //Method for populating the enemy list
    void populateList()
    {
        //Clear existing entries
        foreach (Transform t in contentParent){
            Destroy(t.gameObject);
        }

        //Using the character IO method to get all saved character file paths
        string[] files = CharacterIO.GetSavedEnemyFilePaths();
        if (files == null || files.Length == 0)
        {
            if (emptyText != null) 
            {
                emptyText.gameObject.SetActive(true);
            }
            return;
        }

        //If we have files to show, hide the empty text
        if (emptyText != null) {
            emptyText.gameObject.SetActive(false);
        }

        //Sorting the files alphabetically - since the base enemy files wont actually be altered in game
        Array.Sort(files);

        //Create an entry for each file and setup its data so we can represent it in the list/scrollview
        foreach (string filePath in files)
        {
            try
            {
                //Read the JSON file and parse it into CharacterData
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<EnemyData>(json);

                //Instantiate the item prefab and set it up
                GameObject go = Instantiate(itemPrefab, contentParent, false);
                var item = go.GetComponent<EnemyItem>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to create list item: " + ex.Message);
            }
        }
    }
}

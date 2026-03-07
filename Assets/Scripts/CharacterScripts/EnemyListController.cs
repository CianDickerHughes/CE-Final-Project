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
        PopulateList();
    }

    //Method for populating the enemy list
    void PopulateList()
    {
        //Clear existing entries
        foreach (Transform t in contentParent){
            Destroy(t.gameObject);
        }

        //Using the character IO method to get all saved enemy file paths
        string[] files = CharacterIO.GetSavedEnemyFilePaths();
        Debug.Log($"Found {files?.Length ?? 0} enemy files in {CharacterIO.GetEnemiesFolder()}");
        
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
                //Read the JSON file and parse it into EnemyData
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<EnemyData>(json);

                //Instantiate the item prefab and set it up
                GameObject go = Instantiate(itemPrefab, contentParent, false);
                var item = go.GetComponent<EnemyItem>();
                if (item != null)
                {
                    item.Setup(filePath, data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to create list item: " + ex.Message);
            }
        }
    }

    //Method for only getting the DM enemy - this is used in the DM fight so ill add it here to just call in the controller/manager
    public EnemyData GetDMEnemy()
    {
        //Using the character IO method to get all saved enemy file paths
        string[] files = CharacterIO.GetSavedEnemyFilePaths();
        if (files == null || files.Length == 0)
        {
            Debug.LogError("EnemyListController: No enemy files found in " + CharacterIO.GetEnemiesFolder());
            return null;
        }

        //Look for the DM enemy file - we can identify it by name since we know what we called it when we created it
        string dmEnemyFile = Array.Find(files, f => Path.GetFileNameWithoutExtension(f).Equals("DungeonMaster", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(dmEnemyFile))
        {
            Debug.LogError("EnemyListController: DM enemy file not found in " + CharacterIO.GetEnemiesFolder());
            return null;
        }

        try
        {
            //Read the JSON file and parse it into EnemyData
            string json = File.ReadAllText(dmEnemyFile);
            var data = JsonUtility.FromJson<EnemyData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load DM enemy data: " + ex.Message);
            return null;
        }
    }
}

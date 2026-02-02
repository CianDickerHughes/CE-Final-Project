using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

//Same as the other classes we've seen
//This is meant to higlight the information about enemy items in the game
public class EnemyItem : MonoBehaviour
{
    //Variables for UI References
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyTypeText;
    public TextMeshProUGUI enemyChallengeRatingText;
    public Image enemyIconImage;
    //Same spawn functionalities as we have for characters
    public Button spawnButton;
    private string filePath;
    private EnemyData enemyData;

    //Setup method to initialize the enemy item with data
    public void Setup(string jsonFilePath, EnemyData data)
    {
        filePath = jsonFilePath;
        enemyData = data;

        if (enemyNameText != null)
        {
            enemyNameText.text = string.IsNullOrEmpty(data.name) ? "(Unnamed)" : data.name;
        }

        if (enemyTypeText != null)
        {
            enemyTypeText.text = !string.IsNullOrEmpty(data.type) ? data.type : "";
        }

        if (enemyChallengeRatingText != null)
        {
            enemyChallengeRatingText.text = !string.IsNullOrEmpty(data.challengeRating) ? data.challengeRating : "";
        }

        // load icon image if available (async is not required here for local files)
        if (enemyIconImage != null)
        {
            enemyIconImage.sprite = null;
            enemyIconImage.preserveAspect = true;

            if (!string.IsNullOrEmpty(data.tokenFileName))
            {
                string folder = CharacterIO.GetEnemiesFolder();
                string iconPath = System.IO.Path.Combine(folder, data.tokenFileName);
                if (System.IO.File.Exists(iconPath))
                {
                    try
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(iconPath);
                        Texture2D tex = new Texture2D(2,2);
                        tex.LoadImage(bytes);
                        Sprite sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(0.5f,0.5f));
                        enemyIconImage.sprite = sprite;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error loading enemy icon image: " + ex.Message);
                    }
                }
            }
        }

        //Wiring up the spawn button
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(() => SelectForSpawning(CharacterType.Enemy));
        }
        
    }

    //Method for selecting this enemy for spawning
    void SelectForSpawning(CharacterType type)
    {
        //Making sure the type is enemy
        if (type != CharacterType.Enemy)
        {
            Debug.LogWarning("SelectForSpawning called with non-enemy type on EnemyItem.");
            return;
        }
        if(enemyData != null && GameplayManager.Instance != null){

            GameplayManager.Instance.SetSelectedEnemyForSpawn(enemyData, type);
            Debug.Log($"Selected {enemyData.name} for spawning as {type}");
        }
    }
}

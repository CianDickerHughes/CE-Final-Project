using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This file will operate similarly to the SavedCharacterItem.cs file but for campaign scenes
//Meant to handle individual bits of information about a campaign scene in a list of campaign scenes

public class CampaignSceneItem : MonoBehaviour
{

    //Variables for UI references - the stuff we will actually display from the loaded campaign scenes for the user
    public Image sceneThumbnailImage;
    public TextMeshProUGUI sceneNameText;
    public TextMeshProUGUI lastPlayedText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statusText;
    public Button loadButton;
    public Button deleteButton;
    public Button settingsButton;
    private string filePath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //"Set up" method similar to SavedCharacterItem.cs
    public void Setup(string jsonFilePath, SceneData data, Action<string> onSelectLoad, Action<string> onSelectDelete, Action<string> onSelectSettings)
    {
        //Similar setup process to SavedCharacterItem.cs
        //Where we just initialize the various UI elements based on the data passed in
        filePath = jsonFilePath;

        if (sceneNameText != null)
        {
            sceneNameText.text = string.IsNullOrEmpty(data.sceneName) ? "(Unnamed Scene)" : data.sceneName;
        }

        if (lastPlayedText != null)
        {
            lastPlayedText.text = !string.IsNullOrEmpty(data.lastPlayed) ? data.lastPlayed : "Never Played";
        }

        if (descriptionText != null)
        {
            descriptionText.text = !string.IsNullOrEmpty(data.description) ? data.description : "No Description";
        }

        if (statusText != null)
        {
            statusText.text = !string.IsNullOrEmpty(data.status) ? data.status : "Unknown Status";
        }

        // Set up button callbacks
        //Just helps us set up what happens/actions to take when we click the various buttons on the scene item
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(() => onSelectLoad?.Invoke(filePath));
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => onSelectDelete?.Invoke(filePath));
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => onSelectSettings?.Invoke(filePath));
        }
    }
}

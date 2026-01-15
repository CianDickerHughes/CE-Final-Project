using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

//Script for managing the Scene Maker UI and functionality
//This allows DMs to create and edit scenes for their campaigns
//Handles things such as loading existing scene data, saving changes, and initializing the grid

public class SceneMaker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("UI - Info Display")]
    [SerializeField] private TMP_Text sceneNameText;
    [SerializeField] private TMP_Text sceneTypeText;
    [SerializeField] private TMP_Text gridSizeText;
    [SerializeField] private TMP_Text statusText;

    [Header("UI - Controls")]
    [SerializeField] private TMP_Dropdown tileTypeDropdown;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button clearMapButton;

    [Header("UI - Grid Resize")]
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private Button resizeButton;

    //Private stuff relating to the scene data itself
    private SceneData currentScene;
    private string campaignId;
    private bool isNewScene;

    //Start method for initializing everything
    void Start(){
        //Getting the data from the SceneDataTransfer singleton
        //This will basically just persist between scene loads

        //Initially checking if its not null (error handling)
        if(SceneDataTransfer.Instance == null){
            Debug.LogError("No SceneDataTransfer found");
            statusText.text = "Error: No scene data found.";
            return;
        }

        //Otherwise we get and set the data and fields etc
        currentScene = SceneDataTransfer.Instance.GetPendingScene();
        campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
        isNewScene = !SceneDataTransfer.Instance.IsEditingExistingScene();

        //Setting up the UI fields
        SetupUI();
        SetupDropdown();
        WireButtons();

        //Initializing the grid for the scene
        InitializeGridForScene();

        //Set edit mode on the grid manager
        gridManager.SetEditMode(true);
    }

    //METHODS FOR SETTING UP THE UI
    //Setting up the initial UI fields (Name, type etc)
    private void SetupUI(){
        sceneNameText.text = currentScene.sceneName;
        sceneTypeText.text = currentScene.sceneType.ToString();
        gridSizeText.text = "Grid Size: " + currentScene.mapData.width + " x " + currentScene.mapData.height;
        statusText.text = isNewScene ? "Creating New Scene" : "Editing Existing Scene";
    }

    //Setting up the tile dropdown options
    private void SetupDropdown(){
        //Clearing away all optoins first
        tileTypeDropdown.ClearOptions();
        //Looping through all tile types and adding them as options
        var options = new List<string>();
        foreach(var type in System.Enum.GetValues(typeof(TileType))){
            options.Add(type.ToString());
        }
        tileTypeDropdown.AddOptions(options);
        tileTypeDropdown.onValueChanged.AddListener(OnTileTypeChanged);
    }

    //Method for on tile type changing
    //This will just be useful for when painting tiles
    private void OnTileTypeChanged(int index){
        TileType selectedType = (TileType)index;
        gridManager.SetCurrentPaintType(selectedType);
    }

    //Wiring up the button click events
    private void WireButtons(){
        saveButton.onClick.AddListener(OnSaveClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        clearMapButton.onClick.AddListener(OnClearClicked);
        resizeButton.onClick.AddListener(OnResizeClicked);
    }

    //Initializing the grid based on the current scene's map data
    private void InitializeGridForScene(){
        //If grid is already existing, we load the map data into it
        //If its new we intialize a blank grid
        if(isNewScene){
            gridManager.InitializeGrid(15, 15); //Default size for new scenes
        }
        else{
            gridManager.InitializeGrid(currentScene.mapData.width, currentScene.mapData.height);
            gridManager.LoadMapData(currentScene.mapData);
        }
    }

    //BUTTON CLICK HANDLERS
    //Saving and returning to the Campaign Manager scene (Hopefully this saves things properly)
    void OnSaveClicked()
    {
        // Get map data from grid
        currentScene.mapData = gridManager.SaveMapData();
        
        // Update the scene in SceneDataTransfer
        SceneDataTransfer.Instance.UpdatePendingScene(currentScene);
        
        // Add scene to campaign if new (via CampaignManager)
        if (isNewScene)
        {
            CampaignManager.Instance.AddSceneWithData(currentScene);
        }
        else
        {
            // Update existing scene in campaign
            CampaignManager.Instance.UpdateScene(currentScene);
        }
        
        statusText.text = "Scene saved!";
        
        // Return to Campaign Manager
        SceneManager.LoadScene("CampaignManager"); // Your scene name
    }

    //Canceling without saving
    void OnCancelClicked()
    {
        // Clear pending data
        SceneDataTransfer.Instance.ClearPendingData();
        
        // Return to Campaign Manager without saving
        SceneManager.LoadScene("CampaignManager"); // Your scene name
    }

    //Clearing the grid
    void OnClearClicked(){
        gridManager.ClearMap();
        statusText.text = "Map cleared.";
    }

    void OnResizeClicked()
    {
        if (int.TryParse(widthInput.text, out int newWidth) && 
            int.TryParse(heightInput.text, out int newHeight))
        {
            gridManager.ResizeGrid(newWidth, newHeight);
            gridSizeText.text = "Grid Size: " + newWidth + " x " + newHeight;
            statusText.text = "Grid resized";
        }
        else
        {
            statusText.text = "Invalid dimensions";
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using System;

//This class will help outline and control the basic behaviour we'll need to implement
//This included - if scene is combat or roleplay

//Combat - Turn order management, enemy AI, combat actions
//Roleplay - Movement systems, npc interactions, shop mechanics? or puzzle systems?

//Both - Player management, npc management, movement actions, grid handling etc

//NOTE - CLASS IS INCREDIBLY UNFINISHED AND BASIC
//CIAN WE NEED TO DISCUSS WHAT WE'LL USE TO TRACK TURNS/INITIATIVE ETC
//ALSO HOW WE'LL HANDLE PLAYERS CONTROLS AND ACTIONS - YOU NEED TO FIGURE OUT NETWORKING PLEASE

public enum GameMode { Roleplay, Combat }

public class GameplayManager : MonoBehaviour
{
    public GameMode currentMode;

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Scene Info")]
    private SceneData currentSceneData;
    private string campaignId;

    [Header("Combat Specific State")]
    private List<string> turnOrder;
    private int currentTurnIndex = 0;
    private bool isCombatActive = false;
    public bool isPlayerTurn;

    [Header("Player/NPC Management")]
    //Players linked to their character data
    private List<string> playerIds;
    private Dictionary<string, CharacterData> playerCharacters;

    //Making this class a singleton - all management classes are a singleton because they manage global state
    public static GameplayManager Instance { get; private set; }

    void Awake()
    {
        //Singleton pattern - only one instance persists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameplayManager initialized and will persist across scenes.");
        }
        else
        {
            Debug.Log("GameplayManager duplicate destroyed.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //Loading the scene data using the SceneDataTransfer singleton
        if (SceneDataTransfer.Instance != null)
        {
            currentSceneData = SceneDataTransfer.Instance.GetPendingScene();
            campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            
            //Need to set up the mode of this scene based on the loaded scene type
            currentMode = currentSceneData.sceneType == SceneType.Combat ? GameMode.Combat : GameMode.Roleplay;
        }
        
        //Load the map from the specific scene data
        if (gridManager != null && currentSceneData?.mapData != null)
        {
            gridManager.LoadMapData(currentSceneData.mapData);
            gridManager.SetEditMode(false); //Disable painting in gameplay
        }
        
        // Initialize lists
        turnOrder = new List<string>();
        playerIds = new List<string>();
    }
    
    //Roleplay: Anyone can move anytime
    //Combat: Only current turn player can move
    public bool CanPlayerMove(string playerId)
    {
        if (currentMode == GameMode.Roleplay)
            return true;
            
        // In combat, only current turn player can move
        return GetCurrentTurnPlayerId() == playerId;
    }

    //This method will help us to get the current player turn by their id
    public string GetCurrentTurnPlayerId()
    {
        if (turnOrder == null || turnOrder.Count == 0)
        {
            Debug.LogWarning("Turn order is empty or not initialized.");
            return null;
        }

        if (currentTurnIndex < 0 || currentTurnIndex >= turnOrder.Count)
        {
            Debug.LogWarning("Current turn index is out of bounds.");
            return null;
        }
            
        return turnOrder[currentTurnIndex];
    }

    //Method to stat combat
    //UNFINISHED - CLASSES WILL BE CHANGED SO THIS METHOD IS SUBJECT TO CHANGE
    //NEED TO FIGURE OUT HOW TO CALCULATE INITIATIVE
    public void StartCombat(List<string> participants)
    {
        if (currentMode != GameMode.Combat)
        {
            Debug.LogError("Cannot start combat in Roleplay mode.");
            return;
        }

        turnOrder = new List<string>(participants);
        currentTurnIndex = 0;
        isCombatActive = true;
        Debug.Log("Combat started with participants: " + string.Join(", ", participants));
    }

    //Combat ends
    public void EndCombat()
    {
        //Ending combat and preventing players from moving etc
    }

    //Method to track combat
    public void TrackCombat(){
        Debug.Log("Tracking combat state - looping until combat ends. - checking enemies vs players");
    }
}

public class GameplayUI : MonoBehaviour
{
    //Need to configure the different UI elements for DM and Players
    //POSSIBLY CHANGE THIS - IDK IF THERES A DIFFERENT/BETTER WAY TO DO IT WITH NETWORKING
    [Header("DM Only UI")]
    [SerializeField] private GameObject dmPanel; 
    
    [Header("Player Only UI")]
    [SerializeField] private GameObject playerPanel;  
    
    [Header("Combat Only UI")]
    [SerializeField] private GameObject combatPanel;
    
    [Header("Shared UI")]
    [SerializeField] private GameObject sharedPanel;
    
    void Start()
    {
        //Need to figure out how we're going to do this
    }
}

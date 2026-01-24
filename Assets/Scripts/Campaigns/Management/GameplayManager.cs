using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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

    [Header("Header Information")]
    [SerializeField] private TextMeshProUGUI sceneName;
    [SerializeField] private TextMeshProUGUI playerName;

    [Header("Scene Info")]
    private SceneData currentSceneData;
    private string campaignId;
    //Use this to populate the players name in the header
    private SessionManager sessionManager;

    [Header("Combat Specific State")]
    private List<string> turnOrder;
    private int currentTurnIndex = 0;
    //private bool isCombatActive = false;
    public bool isPlayerTurn;

    [Header("Player/NPC Management")]
    //Players linked to their character data
    private List<string> playerIds;
    private Dictionary<string, CharacterData> playerCharacters;
    private Dictionary<string, Token> playerTokens; // Track spawned tokens

    [Header("DM/Player state management Buttons")]
    [SerializeField] private Button saveAndExitButton;

    [Header("Token Spawining")]
    [SerializeField] private Token tokenPrefab;
    private List<Token> spawnedTokens;

    //Clicking to spawn tokens for players/enemies
    private CharacterData selectedCharacterToSpawn;
    private CharacterType selectedCharacterType;
    private bool isInSpawnMode = false;

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
            // Transfer fresh UI references from this new instance to the persistent singleton
            Instance.TransferUIReferences(this);
            Debug.Log("GameplayManager duplicate destroyed, UI references transferred.");
            Destroy(gameObject);
            return;
        }
    }

    // Called by duplicate instances to transfer their fresh scene references to the singleton
    public void TransferUIReferences(GameplayManager newInstance)
    {
        // Transfer all serialized UI references from the new scene instance
        saveAndExitButton = newInstance.saveAndExitButton;
        sceneName = newInstance.sceneName;
        playerName = newInstance.playerName;
        gridManager = newInstance.gridManager;

        // Re-bind button listener
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.RemoveAllListeners();
            saveAndExitButton.onClick.AddListener(saveAndExit);
            Debug.Log("GameplayManager: Transferred and re-bound saveAndExitButton listener.");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // UI references are now transferred via TransferUIReferences in Awake
        // This is kept for any additional scene-load logic if needed
        Debug.Log($"GameplayManager: Scene '{scene.name}' loaded.");
    }

    private void RebindUIReferences()
    {
        // Re-add listener if button exists (remove first to prevent duplicates)
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.RemoveAllListeners();
            saveAndExitButton.onClick.AddListener(saveAndExit);
            Debug.Log("GameplayManager: Re-bound saveAndExitButton listener.");
        }
    }

    void Start()
    {
        Debug.Log("=== GameplayManager Start() ===");
        
        //Loading the scene data using the SceneDataTransfer singleton
        if (SceneDataTransfer.Instance != null)
        {
            currentSceneData = SceneDataTransfer.Instance.GetPendingScene();
            campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            
            Debug.Log($"SceneDataTransfer found! Scene: {currentSceneData?.sceneName ?? "NULL"}");
            Debug.Log($"MapData: {(currentSceneData?.mapData != null ? $"{currentSceneData.mapData.width}x{currentSceneData.mapData.height}" : "NULL")}");
            
            //Need to set up the mode of this scene based on the loaded scene type
            currentMode = currentSceneData.sceneType == SceneType.Combat ? GameMode.Combat : GameMode.Roleplay;
        }
        else
        {
            Debug.LogError("SceneDataTransfer.Instance is NULL! Make sure it exists in an earlier scene.");
        }
        
        //Load the map from the specific scene data
        if (gridManager != null)
        {
            Debug.Log("GridManager reference is assigned.");
            
            if (currentSceneData?.mapData != null)
            {
                Debug.Log($"Loading map: {currentSceneData.mapData.width}x{currentSceneData.mapData.height}");
                gridManager.LoadMapData(currentSceneData.mapData);
                gridManager.SetEditMode(false); //Disable painting in gameplay
            }
            else
            {
                Debug.LogError("currentSceneData or mapData is NULL - cannot load map!");
            }
        }
        else
        {
            Debug.LogError("GridManager reference is NULL! Assign it in the Inspector.");
        }

        //Setting up buttons for DM/Player actions
        // Use RebindUIReferences to handle button setup (prevents duplicate listeners)
        RebindUIReferences();

        //Setting up the header fields - purely UI related
        if (sceneName != null && currentSceneData != null)
        {
            sceneName.text = currentSceneData.sceneName;
            Debug.Log($"Scene name set to: {currentSceneData.sceneName}");
        }
        else
        {
            Debug.LogWarning("sceneName TextMeshProUGUI or currentSceneData is NULL.");
        }
        //Setting up the session manager to get player info
        sessionManager = SessionManager.Instance;
        if (sessionManager != null && playerName != null)
        {
            playerName.text = sessionManager.CurrentUsername;
            Debug.Log($"Player name set to: {playerName.text}");
        }
        else
        {
            Debug.LogWarning("playerName TextMeshProUGUI or SessionManager instance is NULL.");
        }
        
        // Initialize lists
        turnOrder = new List<string>();
        playerIds = new List<string>();
        playerTokens = new Dictionary<string, Token>();
        
        Debug.Log("=== GameplayManager Start() Complete ===");
    }

    // ==================== UTILITY METHODS ====================
    //Save & Exit - self explanatory where we save current progress and exit back to the campaign manager page
    public void saveAndExit(){
        Debug.Log("Saving current scene data and exiting to Campaign Manager...");
        //Saving the current map data back to the scene data
        if(gridManager != null && currentSceneData != null){
            currentSceneData.mapData = gridManager.SaveMapData();
            Debug.Log("Map data saved to current scene.");
        } else {
            Debug.LogWarning("Cannot save map data - GridManager or currentSceneData is null.");
        }
        //Here we would also save state information relating to this scene
        //For now it just move the dm back to the Campaign Manager scene

        //For now, we just log and move
        Debug.Log("Exiting to Campaign Manager scene...");
        SceneManager.LoadScene("CampaignManager");
    }
    
    // ==================== TOKEN MANAGEMENT ====================
    
    public Token SpawnTokenAtTile(Tile tile, CharacterData character, CharacterType type)
    {
        if (tile == null || tokenPrefab == null) 
        {
            return null;
        }
    
        Token token = Instantiate(tokenPrefab, tile.transform.position, Quaternion.identity);
        token.Inialize(character, type, tile);
        spawnedTokens.Add(token);
        
        return token;
    }

    //Simple method to check if we're in spawn mode
    public bool IsInSpawnMode()
    {
        return isInSpawnMode;
    }

    //UTILITY METHODS FOR TOKEN SPAWINING
    //Setting the selected character and type for spawning
    public void SetSelectedForSpawn(CharacterData data, CharacterType type)
    {
        selectedCharacterToSpawn = data;
        selectedCharacterType = type;
        isInSpawnMode = true;
    }

    //Clearing the selection after spawning - similar to the whole "context" thing we use in this app
    public void ClearSpawnSelection()
    {
        selectedCharacterToSpawn = null;
        isInSpawnMode = false;
    }

    //Attempting to spawn at a specific tile
    public void TrySpawnAtTile(Tile tile)
    {
        if (!isInSpawnMode || tile == null)
        {
            Debug.Log("Not in spawn mode or invalid tile.");
            return;
        }
        
        SpawnTokenAtTile(tile, selectedCharacterToSpawn, selectedCharacterType);
        ClearSpawnSelection();
    }
    
    public bool MoveToken(string playerId, int targetX, int targetY)
    {
        // Check if player can move
        if (!CanPlayerMove(playerId))
        {
            Debug.Log($"Player {playerId} cannot move - not their turn!");
            return false;
        }
        
        // Get the player's token
        if (!playerTokens.TryGetValue(playerId, out Token token))
        {
            Debug.LogWarning($"No token found for player {playerId}");
            return false;
        }
        
        // Get the target tile
        Tile targetTile = gridManager.GetTileAtPosition(new Vector2(targetX, targetY));
        if (targetTile == null)
        {
            Debug.Log("Target position is out of bounds!");
            return false;
        }
        
        // Check if tile is walkable
        if (!targetTile.IsWalkable())
        {
            Debug.Log("Target tile is not walkable!");
            return false;
        }
        
        // Move the token
        token.MoveToTile(targetTile);
        Debug.Log($"Token moved to ({targetX}, {targetY})");
        
        return true;
    }
    
    public Token GetPlayerToken(string playerId)
    {
        playerTokens.TryGetValue(playerId, out Token token);
        return token;
    }
    
    // ==================== MOVEMENT RULES ====================
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
        //isCombatActive = true;
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

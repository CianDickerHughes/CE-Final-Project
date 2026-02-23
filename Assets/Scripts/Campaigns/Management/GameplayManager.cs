using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Services.Authentication;

//This class will help outline and control the basic behaviour we'll need to implement
//This included - if scene is combat or roleplay

//Combat - Turn order management, enemy AI, combat actions
//Roleplay - Movement systems, npc interactions, shop mechanics? or puzzle systems?

//Both - Player management, npc management, movement actions, grid handling etc

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
    private PlayerAssignmentHelper playerAssignmentHelper;

    [Header("Combat Specific State")]
    private List<string> turnOrder;
    private int currentTurnIndex = 0;
    //private bool isCombatActive = false;
    public bool isPlayerTurn;

    [Header("Player/NPC Management")]
    //Players linked to their character data
    private List<string> playerIds;
    private Dictionary<string, CharacterData> playerCharacters;

    [Header("DM/Player state management Buttons")]
    [SerializeField] private Button saveAndExitButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button leaveSessionButton;

    //Making this class a singleton - all management classes are a singleton because they manage global state
    public static GameplayManager Instance { get; private set; }

    void Awake()
    {
        //Singleton pattern - only one instance persists
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            Debug.Log("GameplayManager initialized and will persist across scenes.");
        }
        else
        {
            // Transfer fresh UI references from this new instance to the persistent singleton
            //Instance.TransferUIReferences(this);
            Debug.Log("GameplayManager duplicate destroyed, UI references transferred.");
            Destroy(gameObject);
            return;
        }
    }

    //Simplified Destroy method - testing to see if things will work better
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("GameplayManager instance destroyed.");
        }

        // Unsubscribe from TokenManager events
        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.OnTokensChanged -= AutoSaveTokens;
        }

        // Unsubscribe from scene data updates
        if (SceneDataNetwork.Instance != null)
        {
            SceneDataNetwork.Instance.OnSceneDataReceived -= OnRemoteSceneDataReceived;
        }
    }

    void Start()
    {
        Debug.Log("=== GameplayManager Start() ===");
        
        // Initialize lists early so they're ready for token loading
        turnOrder = new List<string>();
        playerIds = new List<string>();
        playerCharacters = new Dictionary<string, CharacterData>();
        
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

            // Initialize TokenManager FIRST, then load tokens
            if (TokenManager.Instance != null)
            {
                TokenManager.Instance.Initialize(gridManager, playerAssignmentHelper);
                TokenManager.Instance.OnTokensChanged += AutoSaveTokens;
                
                // Load tokens AFTER TokenManager is initialized
                LoadTokensFromSceneData();
            }
            else
            {
                Debug.LogError("TokenManager.Instance is null! Make sure it exists in the scene.");
            }
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

        //Setting up instances for the player assignment helper and network manager
        playerAssignmentHelper = PlayerAssignmentHelper.Instance;
        if(playerAssignmentHelper == null)
        {
            Debug.LogError("PlayerAssignmentHelper instance is null, make sure it exists in the scene");
        }

        // Subscribe to scene data updates from the network (for clients to receive DM changes)
        if (SceneDataNetwork.Instance != null)
        {
            SceneDataNetwork.Instance.OnSceneDataReceived += OnRemoteSceneDataReceived;
            Debug.Log("GameplayManager: Subscribed to SceneDataNetwork updates");
        }
        else
        {
            Debug.LogWarning("GameplayManager: SceneDataNetwork instance not available; won't receive remote scene updates");
        }
        
        Debug.Log("=== GameplayManager Start() Complete ===");
    }

    // Called by duplicate instances to transfer their fresh scene references to the singleton
    public void TransferUIReferences(GameplayManager newInstance)
    {
        // Transfer all serialized UI references from the new scene instance
        saveAndExitButton = newInstance.saveAndExitButton;
        exitButton = newInstance.exitButton;
        leaveSessionButton = newInstance.leaveSessionButton;
        sceneName = newInstance.sceneName;
        playerName = newInstance.playerName;
        gridManager = newInstance.gridManager;

        //Reloading a scene - might fix error when returning to it
        ReinitializeScene();

        // Re-bind button listeners
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.RemoveAllListeners();
            saveAndExitButton.onClick.AddListener(saveAndExit);
            Debug.Log("GameplayManager: Transferred and re-bound saveAndExitButton listener.");
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Exit);
            Debug.Log("GameplayManager: Transferred and re-bound exitButton listener.");
        }
        if (leaveSessionButton != null)
        {
            leaveSessionButton.onClick.RemoveAllListeners();
            leaveSessionButton.onClick.AddListener(LeaveSession);
            Debug.Log("GameplayManager: Transferred and re-bound leaveSessionButton listener.");
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
        // Re-add listeners if buttons exist (remove first to prevent duplicates)
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.RemoveAllListeners();
            saveAndExitButton.onClick.AddListener(saveAndExit);
            Debug.Log("GameplayManager: Re-bound saveAndExitButton listener.");
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Exit);
            Debug.Log("GameplayManager: Re-bound exitButton listener.");
        }
        if (leaveSessionButton != null)
        {
            leaveSessionButton.onClick.RemoveAllListeners();
            leaveSessionButton.onClick.AddListener(LeaveSession);
            Debug.Log("GameplayManager: Re-bound leaveSessionButton listener.");
        }
    }

    //Called when the DM/Host sends updated scene data to clients.
    //Reloads tokens to reflect the current state
    private void OnRemoteSceneDataReceived(SceneData newSceneData)
    {
        if (newSceneData == null)
        {
            Debug.LogWarning("GameplayManager: Received null scene data");
            return;
        }

        Debug.Log($"GameplayManager: Received remote scene update: {newSceneData.sceneName}");

        // Update the local scene data
        currentSceneData = newSceneData;

        // Reload the map/grid from the received scene data
        if (gridManager != null && currentSceneData.mapData != null)
        {
            Debug.Log($"GameplayManager: Reloading map data ({currentSceneData.mapData.width}x{currentSceneData.mapData.height})");
            gridManager.LoadMapData(currentSceneData.mapData);
        }
        else
        {
            Debug.LogWarning($"GameplayManager: Cannot reload map - gridManager null: {gridManager == null}, mapData null: {currentSceneData.mapData == null}");
        }

        // Reload tokens from the updated scene data
        LoadTokensFromSceneData();

        Debug.Log("GameplayManager: Map and tokens reloaded from remote scene data");
    }

    //Arrow key movement for selected token
    void Update()
    {
        if (TokenManager.Instance == null || !TokenManager.Instance.HasSelectedToken() || gridManager == null) 
        {
            return;
        }
        
        // Don't process movement if in spawn mode
        if (TokenManager.Instance.IsInSpawnMode()) 
        {
            return;
        }
        
        Vector2Int direction = Vector2Int.zero;
        
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            direction = Vector2Int.right;
        
        if (direction != Vector2Int.zero)
        {
            Token selectedToken = TokenManager.Instance.GetSelectedToken();
            Tile currentTile = selectedToken.GetCurrentTile();
            if (currentTile != null)
            {
                int newX = currentTile.GridX + direction.x;
                int newY = currentTile.GridY + direction.y;
                Tile targetTile = gridManager.GetTileAtPosition(new Vector2(newX, newY));
                TokenManager.Instance.TryMoveSelectedTokenToTile(targetTile);
            }

            //TokenManager.Instance.DeselectToken();
        }
        
        //Escape to deselect
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TokenManager.Instance.DeselectToken();
        }
    }

    //This should fix the ui issure we're getting with the map not loading
    public void ReinitializeScene()
    {
        Debug.Log("=== GameplayManager ReinitializeScene() ===");

        // Re-loading the map from specific scene data
        if (SceneDataTransfer.Instance != null)
        {
            currentSceneData = SceneDataTransfer.Instance.GetPendingScene();
            campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();
            currentMode = currentSceneData.sceneType == SceneType.Combat ? GameMode.Combat : GameMode.Roleplay;
        }

        // Clear tokens via TokenManager
        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.ClearAllTokens();
        }

        // Loading the map again
        if (gridManager != null && currentSceneData?.mapData != null)
        {
            Debug.Log($"Loading map for scene: {currentSceneData.sceneName}");
            gridManager.LoadMapData(currentSceneData.mapData);
            gridManager.SetEditMode(false);
            LoadTokensFromSceneData();
        }

        //Reloading the UI references
        if(sceneName != null && currentSceneData != null)
        {
            sceneName.text = currentSceneData.sceneName;
        }
        if(playerName != null && sessionManager !=null)
        {
            playerName.text = sessionManager.CurrentUsername;
        }

        RebindUIReferences();

        Debug.Log("Scene reloading complete.");
    }

    // ==================== UTILITY METHODS ====================
    //Leave session - disconnects from network and returns players to campaign manager
    public void LeaveSession()
    {
        Debug.Log("Player leaving session and disconnecting from network...");
        
        // Disconnect from the network
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Network disconnected.");
        }
        
        // Disconnect from Unity Services (Relay, Authentication, etc.)
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Signed out from Unity Services.");
        }
        
        // Return to campaign manager
        Debug.Log("Returning to Campaigns ...");
        SceneManager.LoadScene("Campaigns");
    }
    
    //Exit without saving - simply returns to the campaign manager without saving changes
    public void Exit()
    {
        Debug.Log("Exiting to Campaign Manager without saving...");
        SceneManager.LoadScene("CampaignManager");
    }
    
    //Save & Exit - self explanatory where we save current progress and exit back to the campaign manager page
    //Now uses Compass version control system to commit changes to the main campaign branch
    public void saveAndExit(){
        Debug.Log("Compass: Saving current scene data and exiting to Campaign Manager...");
        //Saving the current map data back to the scene data
        if(gridManager != null && currentSceneData != null){
            currentSceneData.mapData = gridManager.SaveMapData();
            Debug.Log("Compass: Map data saved to current scene.");
            
            //Save all token positions to the scene data
            SaveTokensToSceneData();
            Debug.Log("Compass: Token data saved to current scene.");
            
            //Use Compass to commit changes to the main branch
            CommitWithCompass();
        } else {
            Debug.LogWarning("Compass: Cannot save map data - GridManager or currentSceneData is null.");
        }

        //For now, we just log and move
        Debug.Log("Compass: Exiting to Campaign Manager scene...");
        SceneManager.LoadScene("CampaignManager");
    }
    
    /// <summary>
    /// Uses Compass version control to commit the current scene changes.
    /// This saves to CurrentScene.json (working directory) and commits to the main campaign file.
    /// </summary>
    private void CommitWithCompass()
    {
        if (currentSceneData == null)
        {
            Debug.LogWarning("Compass: Cannot commit - currentSceneData is null.");
            return;
        }
        
        // Get the campaign name for the Compass repository path
        string campaignName = GetCurrentCampaignName();
        if (string.IsNullOrEmpty(campaignName))
        {
            Debug.LogWarning("Compass: Cannot commit - campaign name not found. Falling back to direct save.");
            SaveCurrentSceneToFile();
            return;
        }
        
        // Use CompassManager to save and commit
        if (CompassManager.Instance != null)
        {
            string commitMessage = $"Scene updated: {currentSceneData.sceneName} - {DateTime.Now:HH:mm:ss}";
            CompassCommit commit = CompassManager.Instance.SaveAndCommit(campaignName, currentSceneData, commitMessage);
            
            if (commit != null)
            {
                Debug.Log($"Compass: Committed changes to main branch - {commit.commitId}");
                
                // Broadcast the updated scene to all connected clients after successful commit
                if (SceneDataNetwork.Instance != null)
                {
                    SceneDataNetwork.Instance.SendSceneToClients(currentSceneData);
                    Debug.Log($"Compass: Scene data broadcasted to clients after commit");
                }
                else
                {
                    Debug.LogWarning("Compass: SceneDataNetwork instance not available; scene updates not sent to clients.");
                }
            }
        }
        else
        {
            Debug.LogWarning("Compass: CompassManager not available. Falling back to direct save.");
            SaveCurrentSceneToFile();
        }
    }
    
    /// <summary>
    /// Gets the name of the current campaign.
    /// </summary>
    private string GetCurrentCampaignName()
    {
        if (CampaignManager.Instance != null)
        {
            Campaign campaign = CampaignManager.Instance.GetCurrentCampaign();
            if (campaign != null)
            {
                return campaign.campaignName;
            }
        }
        return null;
    }
    
    //Collect all token positions and save them to currentSceneData
    private void SaveTokensToSceneData()
    {
        if (currentSceneData == null || TokenManager.Instance == null)
        {
            Debug.LogWarning("Cannot save tokens - scene data or TokenManager is null.");
            return;
        }

        List<Token> spawnedTokens = TokenManager.Instance.GetSpawnedTokens();
        
        //Clear existing token data and rebuild
        currentSceneData.tokens = new List<TokenData>();
        
        foreach (Token token in spawnedTokens)
        {
            if (token == null) continue;
            
            Tile currentTile = token.GetCurrentTile();
            if (currentTile == null)
            {
                Debug.LogWarning($"Token {token.name} has no current tile, skipping.");
                continue;
            }
            
            //Get the character or enemy ID based on token type
            string charId = "";
            string charName = "";
            string charClass = "";
            string tokenFileName = "";
            string charDesc = "";
            string enemyId = "";
            CharacterType tokenType = token.getCharacterType();
            
            if (tokenType == CharacterType.Enemy)
            {
                EnemyData enemyData = token.getEnemyData();
                if (enemyData != null)
                {
                    enemyId = enemyData.id;
                    tokenFileName = enemyData.tokenFileName ?? "";
                }
            }
            else
            {
                CharacterData charData = token.getCharacterData();
                if (charData != null)
                {
                    charId = charData.id;
                    charName = charData.charName ?? "";
                    charClass = charData.charClass ?? "";
                    tokenFileName = charData.tokenFileName ?? "";
                    charDesc = charData.race ?? "";
                }
            }
            
            //Create and add the token data with all character info for network transmission
            TokenData tokenData = new TokenData(charId, charName, charClass, tokenFileName, charDesc, enemyId, tokenType, currentTile.GridX, currentTile.GridY);
            currentSceneData.tokens.Add(tokenData);
            
            Debug.Log($"Saved token: Type={tokenType}, CharId={charId}, CharName={charName}, TokenFile={tokenFileName}, Position=({currentTile.GridX}, {currentTile.GridY})");
        }
        
        Debug.Log($"Total tokens saved: {currentSceneData.tokens.Count}");
    }
    
    //Save the current scene data to CurrentScene.json
    private void SaveCurrentSceneToFile()
    {
        if (currentSceneData == null)
        {
            Debug.LogWarning("Cannot save to file - currentSceneData is null.");
            return;
        }
        
        try
        {
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            string currentScenePath = System.IO.Path.Combine(campaignsFolder, "CurrentScene.json");
            
            string json = JsonUtility.ToJson(currentSceneData, true);
            System.IO.File.WriteAllText(currentScenePath, json);
            
            Debug.Log($"Scene data saved to: {currentScenePath}");
            
            //Also update the SceneDataTransfer singleton so other scenes have the updated data
            if (SceneDataTransfer.Instance != null)
            {
                SceneDataTransfer.Instance.UpdatePendingScene(currentSceneData);
            }
            
            // Broadcast the updated scene to all connected clients
            if (SceneDataNetwork.Instance != null)
            {
                SceneDataNetwork.Instance.SendSceneToClients(currentSceneData);
                Debug.Log($"Scene data broadcasted to clients: {currentSceneData.sceneName}");
            }
            else
            {
                Debug.LogWarning("SceneDataNetwork instance not available; scene updates not sent to clients.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save scene to file: {ex.Message}");
        }
    }
    
    //Auto-save tokens whenever they are spawned or moved
    private void AutoSaveTokens()
    {
        SaveTokensToSceneData();
        SaveCurrentSceneToFile();
    }
    
    //Load tokens from the scene data and spawn them on the grid
    private void LoadTokensFromSceneData()
    {
        // Clear existing spawned tokens via TokenManager
        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.ClearAllTokens();
        }

        if (currentSceneData == null || currentSceneData.tokens == null || currentSceneData.tokens.Count == 0)
        {
            Debug.Log("No tokens to load from scene data.");
            return;
        }
        
        Debug.Log($"Loading {currentSceneData.tokens.Count} tokens from scene data...");
        
        foreach (TokenData tokenData in currentSceneData.tokens)
        {
            Tile tile = gridManager.GetTileAtPosition(new Vector2(tokenData.gridX, tokenData.gridY));
            if (tile == null)
            {
                Debug.LogWarning($"Cannot spawn token - no tile at ({tokenData.gridX}, {tokenData.gridY})");
                continue;
            }
            
            if (tokenData.tokenType == CharacterType.Enemy && !string.IsNullOrEmpty(tokenData.enemyId))
            {
                EnemyData enemyData = FindEnemyById(tokenData.enemyId);
                if (enemyData != null)
                {
                    TokenManager.Instance.SpawnEnemyTokenAtTile(tile, enemyData, tokenData.tokenType);
                }
            }
            else if (!string.IsNullOrEmpty(tokenData.characterId))
            {
                CharacterData charData = FindCharacterById(tokenData.characterId);
                if (charData != null)
                {
                    TokenManager.Instance.SpawnTokenAtTile(tile, charData, tokenData.tokenType);
                }
                else if (!string.IsNullOrEmpty(tokenData.characterName))
                {
                    // Character not found locally - create from TokenData fields (network received)
                    Debug.Log($"Character not found locally, creating from TokenData: {tokenData.characterName}");
                    CharacterData networkCharData = new CharacterData();
                    networkCharData.id = tokenData.characterId;
                    networkCharData.charName = tokenData.characterName;
                    networkCharData.charClass = tokenData.characterClass;
                    networkCharData.tokenFileName = tokenData.tokenFileName;
                    
                    TokenManager.Instance.SpawnTokenAtTile(tile, networkCharData, tokenData.tokenType);
                    Debug.Log($"Loaded network character token: {networkCharData.charName} at ({tokenData.gridX}, {tokenData.gridY})");
                }
                else
                {
                    Debug.LogWarning($"Could not find character with ID: {tokenData.characterId} and no TokenData fields available");
                }
            }
        }
    }
    
    //Find a character by ID from all saved character files
    private CharacterData FindCharacterById(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        
        try
        {
            //First check the CharacterManager if available
            if (CharacterManager.Instance != null)
            {
                CharacterData fromManager = CharacterManager.Instance.GetCharacterById(characterId);
                if (fromManager != null) return fromManager;
            }
            
            //Otherwise search through all character JSON files
            string[] files = CharacterIO.GetSavedCharacterFilePaths();
            foreach (string filePath in files)
            {
                string json = System.IO.File.ReadAllText(filePath);
                CharacterData data = JsonUtility.FromJson<CharacterData>(json);
                if (data != null && data.id == characterId)
                {
                    return data;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error finding character by ID: {ex.Message}");
        }
        
        return null;
    }
    
    //Find an enemy by ID from all saved enemy files
    private EnemyData FindEnemyById(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId)) return null;
        
        try
        {
            string[] files = CharacterIO.GetSavedEnemyFilePaths();
            foreach (string filePath in files)
            {
                string json = System.IO.File.ReadAllText(filePath);
                EnemyData data = JsonUtility.FromJson<EnemyData>(json);
                if (data != null && data.id == enemyId)
                {
                    return data;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error finding enemy by ID: {ex.Message}");
        }
        
        return null;
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
}
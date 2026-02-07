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
    private Dictionary<string, Token> playerTokens; // Track spawned tokens
    private Token selectedToken;

    [Header("DM/Player state management Buttons")]
    [SerializeField] private Button saveAndExitButton;

    [Header("Token Spawining")]
    [SerializeField] private Token tokenPrefab;
    private List<Token> spawnedTokens;

    //Clicking to spawn tokens for players/enemies
    private EnemyData selectedEnemyToSpawn;
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
        if(Instance == this)
        {
            Instance = null;
            Debug.Log("GameplayManager instance destroyed.");
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

        //Reloading a scene - might fix error when returning to it
        ReinitializeScene();

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
        
        // Initialize lists early so they're ready for token loading
        turnOrder = new List<string>();
        playerIds = new List<string>();
        playerTokens = new Dictionary<string, Token>();
        spawnedTokens = new List<Token>();
        
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
                
                //Load any saved tokens after the map is ready
                LoadTokensFromSceneData();
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

        //Setting up instances for the player assignment helper and network manager
        playerAssignmentHelper = PlayerAssignmentHelper.Instance;
        if(playerAssignmentHelper == null)
        {
            Debug.LogError("PlayerAssignmentHelper instance is null, make sure it exists in the scene");
        }
        
        Debug.Log("=== GameplayManager Start() Complete ===");
    }

    //Arrow key movement for selected token
    void Update()
    {
        if (selectedToken == null || gridManager == null) return;
        
        //Don't process movement if in spawn mode
        if (isInSpawnMode) return;
        
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
            Tile currentTile = selectedToken.GetCurrentTile();
            if (currentTile != null)
            {
                int newX = currentTile.GridX + direction.x;
                int newY = currentTile.GridY + direction.y;
                Tile targetTile = gridManager.GetTileAtPosition(new Vector2(newX, newY));
                TryMoveSelectedTokenToTile(targetTile);
            }
        }
        
        //Escape to deselect
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectToken();
        }
    }

    //This should fix the ui issure we're getting with the map not loading
    public void ReinitializeScene()
    {
        Debug.Log("=== GameplayManager ReinitializeScene() ===");

        //Re-loading the map from specific scene data
        if(SceneDataTransfer.Instance != null)
        {
            currentSceneData = SceneDataTransfer.Instance.GetPendingScene();
            campaignId = SceneDataTransfer.Instance.GetCurrentCampaignId();

            Debug.Log($"Scene loaded: {currentSceneData?.sceneName ?? "NULL"}");

            currentMode = currentSceneData.sceneType == SceneType.Combat ? GameMode.Combat : GameMode.Roleplay;
        }

        //Clearing the old tokens
        if(spawnedTokens != null)
        {
            foreach (var token in spawnedTokens)
            {
                if(token != null)
                {
                    Destroy(token.gameObject);
                }
                spawnedTokens.Clear();
            }
        }

        //Loading the map again
        if(gridManager != null && currentSceneData?.mapData != null)
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
    //Save & Exit - self explanatory where we save current progress and exit back to the campaign manager page
    public void saveAndExit(){
        Debug.Log("Saving current scene data and exiting to Campaign Manager...");
        //Saving the current map data back to the scene data
        if(gridManager != null && currentSceneData != null){
            currentSceneData.mapData = gridManager.SaveMapData();
            Debug.Log("Map data saved to current scene.");
            
            //Save all token positions to the scene data
            SaveTokensToSceneData();
            Debug.Log("Token data saved to current scene.");
            
            //Persist the scene data to CurrentScene.json
            SaveCurrentSceneToFile();
        } else {
            Debug.LogWarning("Cannot save map data - GridManager or currentSceneData is null.");
        }

        //For now, we just log and move
        Debug.Log("Exiting to Campaign Manager scene...");
        SceneManager.LoadScene("CampaignManager");
    }
    
    //Collect all token positions and save them to currentSceneData
    private void SaveTokensToSceneData()
    {
        if (currentSceneData == null || spawnedTokens == null)
        {
            Debug.LogWarning("Cannot save tokens - scene data or spawned tokens list is null.");
            return;
        }
        
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
            string enemyId = "";
            CharacterType tokenType = token.getCharacterType();
            
            if (tokenType == CharacterType.Enemy)
            {
                EnemyData enemyData = token.getEnemyData();
                if (enemyData != null)
                {
                    enemyId = enemyData.id;
                }
            }
            else
            {
                CharacterData charData = token.getCharacterData();
                if (charData != null)
                {
                    charId = charData.id;
                }
            }
            
            //Create and add the token data
            TokenData tokenData = new TokenData(charId, enemyId, tokenType, currentTile.GridX, currentTile.GridY);
            currentSceneData.tokens.Add(tokenData);
            
            Debug.Log($"Saved token: Type={tokenType}, CharId={charId}, EnemyId={enemyId}, Position=({currentTile.GridX}, {currentTile.GridY})");
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
        if (currentSceneData == null || currentSceneData.tokens == null || currentSceneData.tokens.Count == 0)
        {
            Debug.Log("No tokens to load from scene data.");
            return;
        }
        
        Debug.Log($"Loading {currentSceneData.tokens.Count} tokens from scene data...");
        
        foreach (TokenData tokenData in currentSceneData.tokens)
        {
            //Get the tile at the saved position
            Tile tile = gridManager.GetTileAtPosition(new Vector2(tokenData.gridX, tokenData.gridY));
            if (tile == null)
            {
                Debug.LogWarning($"Cannot spawn token - no tile at ({tokenData.gridX}, {tokenData.gridY})");
                continue;
            }
            
            //Spawn based on token type
            if (tokenData.tokenType == CharacterType.Enemy && !string.IsNullOrEmpty(tokenData.enemyId))
            {
                //Find the enemy data by ID
                EnemyData enemyData = FindEnemyById(tokenData.enemyId);
                if (enemyData != null)
                {
                    SpawnEnemyTokenAtTile(tile, enemyData, tokenData.tokenType);
                    Debug.Log($"Loaded enemy token: {enemyData.name} at ({tokenData.gridX}, {tokenData.gridY})");
                }
                else
                {
                    Debug.LogWarning($"Could not find enemy with ID: {tokenData.enemyId}");
                }
            }
            else if (!string.IsNullOrEmpty(tokenData.characterId))
            {
                //Find the character data by ID
                CharacterData charData = FindCharacterById(tokenData.characterId);
                if (charData != null)
                {
                    SpawnTokenAtTile(tile, charData, tokenData.tokenType);
                    Debug.Log($"Loaded character token: {charData.charName} at ({tokenData.gridX}, {tokenData.gridY})");
                }
                else
                {
                    Debug.LogWarning($"Could not find character with ID: {tokenData.characterId}");
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
    
    // ==================== TOKEN MANAGEMENT ====================
    
    public Token SpawnTokenAtTile(Tile tile, CharacterData character, CharacterType type)
    {
        if (tile == null || tokenPrefab == null) 
        {
            return null;
        }
    
        Token token = Instantiate(tokenPrefab, new Vector3(tile.transform.position.x, tile.transform.position.y, -1), Quaternion.identity);
        token.Initialize(character, type, tile);
        spawnedTokens.Add(token);

        Debug.Log("Character Spawned: " + character.charName + " at Tile (" + token.transform.position + ")");
        
        return token;
    }

    //Spawning enemies method (tokens)
    public Token SpawnEnemyTokenAtTile(Tile tile, EnemyData enemy, CharacterType type)
    {
        if (tile == null || tokenPrefab == null) 
        {
            return null;
        }
    
        Token token = Instantiate(tokenPrefab, new Vector3(tile.transform.position.x, tile.transform.position.y, -1), Quaternion.identity);
        token.Initialize(enemy, type, tile);
        spawnedTokens.Add(token);

        Debug.Log("Enemy Spawned: " + enemy.name + " at Tile (" + token.transform.position + ")");
        
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

    //Method for spawning enemies - same as one above but for enemies
    //Could potentially have an overloaded method but for clarity having two separate methods is better
    public void SetSelectedEnemyForSpawn(EnemyData data, CharacterType type)
    {
        //Add in behaviour soon 
        selectedEnemyToSpawn = data;
        selectedCharacterType = type;
        isInSpawnMode = true;
    }

    //Clearing the selection after spawning - similar to the whole "context" thing we use in this app
    public void ClearSpawnSelection()
    {
        selectedCharacterToSpawn = null;
        selectedEnemyToSpawn = null;
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
        
        //Check if tile is walkable before spawning
        if (!tile.IsWalkable())
        {
            Debug.Log($"Cannot spawn on non-walkable tile at ({tile.GridX}, {tile.GridY})");
            ClearSpawnSelection();
            return;
        }

        //Selecting a character to spawn
        if(selectedCharacterToSpawn != null)
        {
            SpawnTokenAtTile(tile, selectedCharacterToSpawn, selectedCharacterType);
            ClearSpawnSelection();
            AutoSaveTokens(); //Auto-save after spawning
            return;
        }
        //Selecting an enemy to spawn
        else if(selectedEnemyToSpawn != null)
        {
            SpawnEnemyTokenAtTile(tile, selectedEnemyToSpawn, selectedCharacterType);
            ClearSpawnSelection();
            AutoSaveTokens(); //Auto-save after spawning
            return;
        }
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
        
        AutoSaveTokens(); //Auto-save after moving
        
        return true;
    }
    
    public Token GetPlayerToken(string playerId)
    {
        playerTokens.TryGetValue(playerId, out Token token);
        return token;
    }

    // ==================== TOKEN SELECTION & MOVEMENT ====================
    
    //Select a token for movement
    public void SelectToken(Token t)
    {
        //Deselect previous if any
        if (selectedToken != null)
        {
            selectedToken.SetSelected(false);
        }
        selectedToken = t;
        if (selectedToken != null)
        {
            //Verifying the caller/player can control this token before we select it
            if(CanCurrentPlayerControlToken(selectedToken))
            {
                selectedToken.SetSelected(true);
                Debug.Log($"Token selected: {selectedToken.name}");
            }
            else
            {
                Debug.Log("Cant select token - player doesnt have control!");
                return;
            }
        }
    }

    //Utility method for seeing if the current player control the token
    //Gets character id from the token and checks if the player has control over that character
    public bool CanCurrentPlayerControlToken(Token token){
        if(token == null)
        {
            return false;
        }

        //Ensuring the host/dm can control all tokens
        if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            return true;
        }

        //Retrieving the character data from the token to check ownership
        CharacterData characterData = token.getCharacterData();
        if(characterData != null)
        {
            //Returning whether player can control this token - ownership
            return playerAssignmentHelper.CanControlCharacter(characterData.id);
        }

        return false;
    }
    
    //Deselect current token
    public void DeselectToken()
    {
        if (selectedToken != null)
        {
            selectedToken.SetSelected(false);
            Debug.Log($"Token deselected: {selectedToken.name}");
        }
        selectedToken = null;
    }
    
    //Check if we have a token selected
    public bool HasSelectedToken()
    {
        return selectedToken != null;
    }
    
    //Try to move selected token to a tile
    public void TryMoveSelectedTokenToTile(Tile tile)
    {
        if (selectedToken == null || tile == null)
        {
            return;
        }

        //Checking if a player can move this token
        if(!CanCurrentPlayerControlToken(selectedToken))
        {
            Debug.Log("Cant move token - player doesnt have control");
            return;
        }
        
        //Check if tile is walkable
        if (!tile.IsWalkable())
        {
            Debug.Log("Cannot move to non-walkable tile.");
            return;
        }
        
        selectedToken.MoveToTile(tile);
        Debug.Log($"Moved {selectedToken.name} to tile ({tile.GridX}, {tile.GridY})");
        
        AutoSaveTokens(); //Auto-save after moving
    }

    public Token GetTokenForCharacter(string characterId)
    {
        foreach(var token in spawnedTokens)
        {
            if(token.getCharacterData() != null && token.getCharacterData().id == characterId)
            {
                return token;
            }
        }

        Debug.LogWarning($"No token found for character ID: {characterId}");
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
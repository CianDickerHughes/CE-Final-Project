using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

//This class ie meant to be a split of the original gameplay manager
//This one only handles the logic for spawning tokens etc - just moving the methods over
public class TokenManager : MonoBehaviour
{
    //Singleton for easy access and consistency
    public static TokenManager Instance { get; private set;}

    //Variables for the tokens management
    [Header("Token Spawining")]
    [SerializeField] private Token tokenPrefab;
    private List<Token> spawnedTokens;
    private Dictionary<string, Token> playerTokens;
    private Token selectedToken;

    //Clicking to spawn tokens for players/enemies
    private EnemyData selectedEnemyToSpawn;
    private CharacterData selectedCharacterToSpawn;
    private CharacterType selectedCharacterType;
    private bool isInSpawnMode = false;

    //References for player assignment and gird management
    private GridManager gridManager;
    private PlayerAssignmentHelper playerAssignmentHelper;

    //Combat manager reference for turn logic
    //private CombatManager combatManager = CombatManager.Instance;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            spawnedTokens = new List<Token>();
            playerTokens = new Dictionary<string, Token>();
            Debug.Log("TokenManager instance created");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    //Initialize method to set things up - like constructor
    public void Initialize(GridManager grid, PlayerAssignmentHelper assignmentHelper)
    {
        gridManager = grid;
        playerAssignmentHelper = assignmentHelper;
        Debug.Log("TokenManager: References initialized.");
    }

    public event System.Action OnTokensChanged;


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

    //Simple method to check if we're in spawn mode
    public bool IsInSpawnMode()
    {
        return isInSpawnMode;
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
            OnTokensChanged?.Invoke();
            return;
        }
        //Selecting an enemy to spawn
        else if(selectedEnemyToSpawn != null)
        {
            SpawnEnemyTokenAtTile(tile, selectedEnemyToSpawn, selectedCharacterType);
            ClearSpawnSelection();
            OnTokensChanged?.Invoke();
            return;
        }
    }

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
            //AND verifying we can move this token
            if(CanCurrentPlayerControlToken(selectedToken) && CanTokenMoveNow(selectedToken))
            {
                selectedToken.SetSelected(true);
            }
            else
            {
                selectedToken = null;
                return;
            }
        }
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
            return;
        }

        if(!CanTokenMoveNow(selectedToken))
        {
            return;
        }
        
        //Check if tile is walkable
        if (!tile.IsWalkable())
        {
            return;
        }
        
        selectedToken.MoveToTile(tile);

        OnTokensChanged?.Invoke();
    }

    private bool CanTokenMoveNow(Token token)
    {
        //If CombatManager doesn't exist or combat isn't active, allow movement
        if (CombatManager.Instance == null)
        {
            return true;
        }
        
        return CombatManager.Instance.CanTokenMove(token);
    }

    public bool MoveToken(string playerId, int targetX, int targetY)
    {
        if (gridManager == null)
        {
            Debug.LogError("TokenManager: GridManager reference is null!");
            return false;
        }

        // Check if player can move - delegate to GameplayManager for turn logic
        if (GameplayManager.Instance != null && !GameplayManager.Instance.CanPlayerMove(playerId))
        {
            Debug.Log($"Player {playerId} cannot move - not their turn!");
            return false;
        }
        
        if (!playerTokens.TryGetValue(playerId, out Token token))
        {
            Debug.LogWarning($"No token found for player {playerId}");
            return false;
        }
        
        Tile targetTile = gridManager.GetTileAtPosition(new Vector2(targetX, targetY));
        if (targetTile == null)
        {
            Debug.Log("Target position is out of bounds!");
            return false;
        }
        
        if (!targetTile.IsWalkable())
        {
            Debug.Log("Target tile is not walkable!");
            return false;
        }
        
        token.MoveToTile(targetTile);
        Debug.Log($"Token moved to ({targetX}, {targetY})");
        
        OnTokensChanged?.Invoke();
        
        return true;
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

        //Local testing fallback - if no network is active, allow all token control
        if(NetworkManager.Singleton == null)
        {
            return true;
        }
        
        //If network exists but not connected (local testing), allow control
        if(!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            return true;
        }

        //For enemy tokens in networked game, only server/DM can control
        if(token.getCharacterType() == CharacterType.Enemy)
        {
            return false;
        }

        //Retrieving the character data from the token to check ownership
        CharacterData characterData = token.getCharacterData();
        if(characterData != null && playerAssignmentHelper != null)
        {
            //Returning whether player can control this token - ownership
            return playerAssignmentHelper.CanControlCharacter(characterData.id);
        }

        return false;
    }

    public List<Token> GetSpawnedTokens()
    {
        return spawnedTokens;
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

    public Token GetPlayerToken(string playerId)
    {
        playerTokens.TryGetValue(playerId, out Token token);
        return token;
    }

    public void ClearAllTokens()
    {
        if (spawnedTokens != null)
        {
            foreach (var token in spawnedTokens)
            {
                if (token != null)
                {
                    Destroy(token.gameObject);
                }
            }
            spawnedTokens.Clear();
        }
        playerTokens?.Clear();
        selectedToken = null;
    }

    public Token GetSelectedToken() => selectedToken;
}

using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//Manages the setup of the DM Fight scene.
//Spawns the player's selected character and the DM enemy on opposite sides of the arena
public class DMFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TokenManager tokenManager;
    [SerializeField] private Button exitButton;

    [Header("DM Enemy Settings - Fallback if file not found")]
    [SerializeField] private string dmEnemyName = "Dungeon Master";
    [SerializeField] private int dmEnemyHP = 50;
    [SerializeField] private int dmEnemyAC = 15;
    [SerializeField] private int dmEnemySpeed = 30;

    [Header("Spawn Positions")]
    [Tooltip("Player spawns at left side")]
    [SerializeField] private Vector2Int playerSpawnOffset = new Vector2Int(1, 5);
    [Tooltip("Enemy spawns at right side")]
    [SerializeField] private Vector2Int enemySpawnOffset = new Vector2Int(8, 5);

    private bool hasSetupCompleted = false;

    void Start()
    {
        Debug.Log("=== DMFightManager Start() ===");

        //Setup exit button if assigned
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMainMenu);
        }

        //Creating the MapData here for the DM fight - this makes things simpler and prevents an infinite loop I encountered earlier
        MapData mapData = new MapData(10, 10);
        for (int x = 0; x < mapData.width; x++)
        {
            for (int y = 0; y < mapData.height; y++)
            {
                mapData.SetTileAt(x, y, TileType.Grass);
            }
        }
        //Initialize the grid with our map data
        if (gridManager != null)
        {
            gridManager.LoadMapData(mapData);
        }
        else
        {
            Debug.LogError("DMFightManager: GridManager reference is missing!");
            return;
        }

        //Initialize token manager with grid reference
        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.Initialize(gridManager, null);
        }
        else
        {
            Debug.LogError("DMFightManager: TokenManager.Instance is null!");
            return;
        }

        //Now spawn the characters
        SpawnCharacters();
    }

    //Main setup method - spawns player and enemy tokens
    private void SpawnCharacters()
    {
        if (hasSetupCompleted)
        {
            Debug.LogWarning("DMFightManager: Setup already completed!");
            return;
        }

        //Get the player's selected character
        CharacterData playerCharacter = CharacterSelectionContext.GetSelectedCharacter();
        if (playerCharacter == null)
        {
            Debug.LogError("DMFightManager: No character selected! Cannot setup fight.");
            return;
        }

        Debug.Log($"DMFightManager: Setting up fight for {playerCharacter.charName}");

        //Initialize token manager with grid reference
        if (TokenManager.Instance != null && gridManager != null)
        {
            TokenManager.Instance.Initialize(gridManager, null);
        }
        else
        {
            Debug.LogError("DMFightManager: TokenManager or GridManager is missing!");
            return;
        }

        //Spawn the player token on the left side
        Tile playerTile = gridManager.GetTileAtPosition(new Vector2(playerSpawnOffset.x, playerSpawnOffset.y));
        if (playerTile != null)
        {
            Token playerToken = TokenManager.Instance.SpawnTokenAtTile(playerTile, playerCharacter, CharacterType.Player);
            if (playerToken != null)
            {
                Debug.Log($"DMFightManager: Spawned player '{playerCharacter.charName}' at ({playerSpawnOffset.x}, {playerSpawnOffset.y})");
            }
        }
        else
        {
            Debug.LogError($"DMFightManager: Could not find tile at player spawn position ({playerSpawnOffset.x}, {playerSpawnOffset.y})");
        }

        //Load and spawn the DM enemy on the right side
        EnemyData dmEnemy = LoadDMEnemyFromFile();
        Tile enemyTile = gridManager.GetTileAtPosition(new Vector2(enemySpawnOffset.x, enemySpawnOffset.y));
        if (enemyTile != null && dmEnemy != null)
        {
            Token enemyToken = TokenManager.Instance.SpawnEnemyTokenAtTile(enemyTile, dmEnemy, CharacterType.Enemy);
            if (enemyToken != null)
            {
                Debug.Log($"DMFightManager: Spawned DM enemy '{dmEnemy.name}' at ({enemySpawnOffset.x}, {enemySpawnOffset.y})");
            }
        }
        else
        {
            Debug.LogError($"DMFightManager: Could not find tile at enemy spawn position ({enemySpawnOffset.x}, {enemySpawnOffset.y})");
        }

        hasSetupCompleted = true;
        Debug.Log("DMFightManager: Fight setup complete!");

        //Clear the selection context since we've used it
        CharacterSelectionContext.Clear();
    }

    //Loads the DM enemy data from the DungeonMaster.json file
    private EnemyData LoadDMEnemyFromFile()
    {
        try
        {
            string enemiesFolder = CharacterIO.GetEnemiesFolder();
            string dmFilePath = Path.Combine(enemiesFolder, "DungeonMaster.json");

            if (File.Exists(dmFilePath))
            {
                string json = File.ReadAllText(dmFilePath);
                EnemyData enemy = JsonUtility.FromJson<EnemyData>(json);
                Debug.Log($"DMFightManager: Loaded DM enemy '{enemy.name}' from file - HP: {enemy.HP}, AC: {enemy.AC}");
                return enemy;
            }
            else
            {
                Debug.LogWarning($"DMFightManager: DungeonMaster.json not found at {dmFilePath}, using fallback");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DMFightManager: Failed to load DM enemy - {ex.Message}");
        }

        //Fallback to hardcoded values if file not found
        return new EnemyData
        {
            name = dmEnemyName,
            HP = dmEnemyHP,
            AC = dmEnemyAC,
            speed = dmEnemySpeed,
            type = "Humanoid",
            enemyType = EnemyType.Adaptive
        };
    }

    //Exit back to main menu
    private void ExitToMainMenu()
    {
        Debug.Log("DMFightManager: Returning to Campaigns...");
        SceneManager.LoadScene("Campaigns");
    }
}

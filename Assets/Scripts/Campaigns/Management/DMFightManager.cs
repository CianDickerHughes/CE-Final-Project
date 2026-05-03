using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Sets up the DM Fight scene:
///   1. Builds a 10×10 grass map.
///   2. Spawns the player's selected character and the DM enemy.
///   3. Overrides the CombatLogger path so AIManager can find the log
///      even without a campaign being loaded.
///   4. Starts combat automatically.
/// </summary>
public class DMFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TokenManager tokenManager;
    [SerializeField] private Button exitButton;

    [Header("DM Enemy Settings — fallback if file not found")]
    [SerializeField] private string dmEnemyName   = "Dungeon Master";
    [SerializeField] private int    dmEnemyHP     = 50;
    [SerializeField] private int    dmEnemyAC     = 15;
    [SerializeField] private int    dmEnemySpeed  = 30;

    [Header("Spawn Positions")]
    [Tooltip("Player spawns here (left side)")]
    [SerializeField] private Vector2Int playerSpawnOffset = new Vector2Int(1, 5);
    [Tooltip("DM enemy spawns here (right side)")]
    [SerializeField] private Vector2Int enemySpawnOffset  = new Vector2Int(8, 5);

    private bool _setupComplete = false;

    void Start()
    {
        Debug.Log("=== DMFightManager Start() ===");

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMainMenu);
        }

        // ── Override the combat log path so AIManager can find it ──
        // This must happen BEFORE StartLogging() is called.
        string logPath = Path.Combine(Application.persistentDataPath, "CombatLogs", "combat_log.json");
        CombatLogger.Instance.SetLogPath(logPath);
        Debug.Log($"DMFightManager: Combat log path set to {logPath}");

        // ── Build the arena map ────────────────────────────────────
        MapData mapData = new MapData(10, 10);
        for (int x = 0; x < mapData.width; x++)
            for (int y = 0; y < mapData.height; y++)
                mapData.SetTileAt(x, y, TileType.Grass);

        if (gridManager != null)
            gridManager.LoadMapData(mapData);
        else
        {
            Debug.LogError("DMFightManager: GridManager reference is missing!");
            return;
        }

        if (TokenManager.Instance != null)
            TokenManager.Instance.Initialize(gridManager, null);
        else
        {
            Debug.LogError("DMFightManager: TokenManager.Instance is null!");
            return;
        }

        SpawnCharacters();
    }

    // ─────────────────────────────────────────────────────────────

    private void SpawnCharacters()
    {
        if (_setupComplete) return;

        CharacterData playerCharacter = CharacterSelectionContext.GetSelectedCharacter();
        if (playerCharacter == null)
        {
            Debug.LogError("DMFightManager: No character selected! Cannot setup fight.");
            return;
        }

        Debug.Log($"DMFightManager: Setting up fight for {playerCharacter.charName}");

        // Spawn player (left side)
        Tile playerTile = gridManager.GetTileAtPosition(
            new Vector2(playerSpawnOffset.x, playerSpawnOffset.y));

        if (playerTile != null)
        {
            Token playerToken = TokenManager.Instance.SpawnTokenAtTile(playerTile, playerCharacter, CharacterType.Player);
            if (playerToken != null)
                Debug.Log($"DMFightManager: Spawned player '{playerCharacter.charName}'");
        }
        else
        {
            Debug.LogError($"DMFightManager: No tile at player spawn ({playerSpawnOffset.x},{playerSpawnOffset.y})");
        }

        // Spawn DM enemy (right side)
        EnemyData dmEnemy = LoadDMEnemyFromFile();
        Tile enemyTile = gridManager.GetTileAtPosition(
            new Vector2(enemySpawnOffset.x, enemySpawnOffset.y));

        if (enemyTile != null && dmEnemy != null)
        {
            Token enemyToken = TokenManager.Instance.SpawnDMEnemyTokenAtTile(enemyTile, dmEnemy, CharacterType.Enemy);
            if (enemyToken != null)
                Debug.Log($"DMFightManager: Spawned DM enemy '{dmEnemy.name}'");
        }
        else
        {
            Debug.LogError($"DMFightManager: No tile at enemy spawn ({enemySpawnOffset.x},{enemySpawnOffset.y})");
        }

        _setupComplete = true;

        // ── Start combat ─────────────────────────────────────────
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.PopulateCombatList();
            CombatManager.Instance.controlCombat();   // rolls initiative, starts logging, triggers AI reset
            Debug.Log("DMFightManager: Combat started.");
        }
        else
        {
            Debug.LogError("DMFightManager: CombatManager not found in scene!");
        }

        CharacterSelectionContext.Clear();
    }

    // ─────────────────────────────────────────────────────────────

    private EnemyData LoadDMEnemyFromFile()
    {
        try
        {
            string dmFilePath = Path.Combine(CharacterIO.GetEnemiesFolder(), "DungeonMaster.json");
            if (File.Exists(dmFilePath))
            {
                EnemyData enemy = JsonUtility.FromJson<EnemyData>(File.ReadAllText(dmFilePath));
                Debug.Log($"DMFightManager: Loaded DM enemy '{enemy.name}' from file");
                return enemy;
            }
            Debug.LogWarning($"DMFightManager: DungeonMaster.json not found, using fallback.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DMFightManager: Failed to load DM enemy — {ex.Message}");
        }

        return new EnemyData
        {
            name      = dmEnemyName,
            HP        = dmEnemyHP,
            AC        = dmEnemyAC,
            speed     = dmEnemySpeed,
            type      = "Humanoid",
            enemyType = EnemyType.Adaptive
        };
    }

    private void ExitToMainMenu()
    {
        Debug.Log("DMFightManager: Returning to Campaigns...");
        SceneManager.LoadScene("Campaigns");
    }
}
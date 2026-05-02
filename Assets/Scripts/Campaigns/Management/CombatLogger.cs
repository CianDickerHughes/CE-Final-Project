using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Logs all combat actions for AI training data or replay.
/// Tracks damage, healing, movement, and positions.
/// </summary>
public class CombatLogger : MonoBehaviour
{
    private static CombatLogger _instance;
    public static CombatLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindAnyObjectByType<CombatLogger>();
                
                // Auto-create if none exists
                if (_instance == null)
                {
                    GameObject go = new GameObject("CombatLogger");
                    _instance = go.AddComponent<CombatLogger>();
                    Debug.Log("CombatLogger: Auto-created CombatLogger instance");
                }
            }
            return _instance;
        }
    }

    [Header("Settings")]
    [SerializeField] private string logFolder = "CombatLogs";

    private CombatLog currentLog;
    private string currentLogFilePath;  // Persistent file path for this combat session
    private bool isLogging = false;
    private List<CombatAction> pendingTurnActions = new List<CombatAction>();  // Buffer for current turn (non-movement actions)
    private Dictionary<string, int> pendingHPChanges = new Dictionary<string, int>();  // HP changes for current turn
    
    // Movement tracking - stores original position and current position per actor
    private Dictionary<string, GridPosition> movementOriginalPos = new Dictionary<string, GridPosition>();
    private Dictionary<string, GridPosition> movementCurrentPos = new Dictionary<string, GridPosition>();

    private string _overrideLogPath = null;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// NEW — call before StartLogging() in scenes with no campaign (e.g. DMFight).
    /// Both CombatLogger and AIManager will use this path so they stay in sync.
    ///
    /// Recommended path:
    ///   Path.Combine(Application.persistentDataPath, "CombatLogs", "combat_log.json")
    /// </summary>
    public void SetLogPath(string fullFilePath)
    {
        _overrideLogPath = fullFilePath;
        Debug.Log($"CombatLogger: Log path overridden to {fullFilePath}");
    }

    /// <summary>
    /// Start logging a new combat session - creates file immediately
    /// </summary>
    public void StartLogging()
    {
        currentLog = new CombatLog
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            participants = new List<ParticipantInfo>(),
            hp_summary = new Dictionary<string, int>(),
            action_log = new List<CombatAction>()
        };
        
        // Populate participant info from CombatManager
        if (CombatManager.Instance != null)
        {
            foreach (var participant in CombatManager.Instance.GetInitiativeOrder())
            {
                if (participant.token == null) continue;
                
                currentLog.participants.Add(new ParticipantInfo
                {
                    name = participant.GetName(),
                    max_hp = participant.maxHP,
                    ac = participant.GetAC()
                });
            }
        }
        
        isLogging = true;
        
        // Create the file immediately
        string folderPath = GetCombatLogsFolder();
        if (!string.IsNullOrEmpty(folderPath))
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            currentLogFilePath = Path.Combine(folderPath, "combat_log.json");
            SaveLog();  // Create initial file
            Debug.Log($"CombatLogger: Started logging to {currentLogFilePath}");
        }
        else
        {
            Debug.LogWarning("CombatLogger: Could not get campaign folder path - is a campaign loaded?");
        }
    }

    /// <summary>
    /// Stop logging
    /// </summary>
    public void StopLogging(bool save = true)
    {
        // Commit any pending actions before stopping
        if (pendingTurnActions.Count > 0 || movementOriginalPos.Count > 0)
        {
            CommitTurn();
        }
        
        if (save && currentLog != null)
        {
            SaveLog();  // Final save
        }
        isLogging = false;
        currentLogFilePath = null;
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();
        movementOriginalPos.Clear();
        movementCurrentPos.Clear();
    }

    /// <summary>
    /// Commit all pending actions from the current turn to the log.
    /// Call this when the player clicks "End Turn".
    /// </summary>
    public void CommitTurn()
    {
        if (!isLogging || currentLog == null)
        {
            Debug.LogWarning($"CombatLogger.CommitTurn: Not logging - isLogging={isLogging}, currentLog={currentLog != null}");
            return;
        }
        
        Debug.Log($"CombatLogger.CommitTurn: pendingTurnActions={pendingTurnActions.Count}, movementOriginalPos={movementOriginalPos.Count}");
        
        // If there are NO damage/healing/ability actions, log movement only
        if (pendingTurnActions.Count == 0 && movementOriginalPos.Count > 0)
        {
            // Log movement for each actor that moved
            foreach (var actor in movementOriginalPos.Keys)
            {
                if (movementCurrentPos.ContainsKey(actor))
                {
                    var originalPos = movementOriginalPos[actor];
                    var finalPos = movementCurrentPos[actor];
                    
                    // Only log if they actually moved to a different position
                    if (originalPos.gridX != finalPos.gridX || originalPos.gridY != finalPos.gridY)
                    {
                        var movementAction = new CombatAction
                        {
                            type = "movement",
                            actor = actor,
                            positions = new ActionPositions
                            {
                                source = originalPos,
                                target = finalPos
                            }
                        };
                        currentLog.action_log.Add(movementAction);
                    }
                }
            }
        }
        else
        {
            // Add all pending non-movement actions to the main log
            foreach (var action in pendingTurnActions)
            {
                currentLog.action_log.Add(action);
            }
        }
        
        // Update HP summary with pending changes
        foreach (var kvp in pendingHPChanges)
        {
            UpdateHPSummary(kvp.Key, kvp.Value);
        }
        
        // Clear pending buffers
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();
        movementOriginalPos.Clear();
        movementCurrentPos.Clear();
        
        // Save to file
        SaveLog();
        Debug.Log($"CombatLogger: Committed turn with {currentLog.action_log.Count} total actions");
    }

    /// <summary>
    /// Log a damage action - replaces any previous action this turn (1 action per turn)
    /// </summary>
    public void LogDamage(string source, string target, int hpRemoved, string description, 
                          GridPosition sourcePos, GridPosition targetPos)
    {
        if (!isLogging || currentLog == null)
        {
            Debug.LogWarning($"CombatLogger.LogDamage: Not logging - isLogging={isLogging}, currentLog={currentLog != null}");
            return;
        }

        // Clear previous actions - only one action per turn
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();

        var action = new CombatAction
        {
            type = "damage",
            source = source,
            target = target,
            hp_removed = hpRemoved,
            description = description,
            positions = new ActionPositions
            {
                source = sourcePos,
                target = targetPos
            }
        };

        pendingTurnActions.Add(action);
        AddPendingHPChange(target, -hpRemoved);
    }

    /// <summary>
    /// Log a healing action - replaces any previous action this turn (1 action per turn)
    /// </summary>
    public void LogHealing(string source, string target, int hpRestored,
                           GridPosition sourcePos, GridPosition targetPos, string description = "")
    {
        if (!isLogging || currentLog == null) return;

        // Clear previous actions - only one action per turn
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();

        var action = new CombatAction
        {
            type = "healing",
            source = source,
            target = target,
            hp_restored = hpRestored,
            description = description,
            positions = new ActionPositions
            {
                source = sourcePos,
                target = targetPos
            }
        };

        pendingTurnActions.Add(action);
        AddPendingHPChange(target, hpRestored);
    }

    //Log a death event - when a participant is killed in combat
    public void LogDeath(string victimName, string victimId)
    {
        if (!isLogging || currentLog == null) return;

        var action = new CombatAction
        {
            type = "death",
            target = victimName,
            description = $"{victimName} was defeated"
        };

        //Death is always logged immediately as it's a significant event
        currentLog.action_log.Add(action);
        
        Debug.Log($"CombatLogger: Logged death of {victimName}");
        
        //Save immediately to ensure death is recorded
        SaveLog();
    }

    /// <summary>
    /// Log a movement action - only tracks start and end positions.
    /// Movement is only logged if no other actions (damage/healing/ability) are performed.
    /// </summary>
    public void LogMovement(string actor, GridPosition from, GridPosition to)
    {
        if (!isLogging || currentLog == null) return;

        // Track original position (first time this actor moves this turn)
        if (!movementOriginalPos.ContainsKey(actor))
        {
            movementOriginalPos[actor] = from;
        }
        
        // Always update current position to latest
        movementCurrentPos[actor] = to;
    }

    /// <summary>
    /// Log an ability use - replaces any previous action this turn (1 action per turn)
    /// </summary>
    public void LogAbility(string source, string target, string abilityName, int effect,
                           bool isDamage, GridPosition sourcePos, GridPosition targetPos)
    {
        if (!isLogging || currentLog == null) return;

        // Clear previous actions - only one action per turn
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();

        var action = new CombatAction
        {
            type = isDamage ? "damage" : "healing",
            source = source,
            target = target,
            description = $"Used {abilityName}",
            positions = new ActionPositions
            {
                source = sourcePos,
                target = targetPos
            }
        };

        if (isDamage)
        {
            action.hp_removed = effect;
            AddPendingHPChange(target, -effect);
        }
        else
        {
            action.hp_restored = effect;
            AddPendingHPChange(target, effect);
        }

        pendingTurnActions.Add(action);
    }

    /// <summary>
    /// Log a buff/utility ability use (no HP change) - replaces any previous action this turn
    /// </summary>
    public void LogAbilityUse(string source, string abilityName, GridPosition sourcePos, string target = null, GridPosition targetPos = default)
    {
        if (!isLogging || currentLog == null) return;

        // Clear previous actions - only one action per turn
        pendingTurnActions.Clear();
        pendingHPChanges.Clear();

        var action = new CombatAction
        {
            type = "ability",
            source = source,
            target = target ?? source,
            description = $"Used {abilityName}",
            positions = new ActionPositions
            {
                source = sourcePos,
                target = targetPos ?? sourcePos
            }
        };

        pendingTurnActions.Add(action);
    }

    private void AddPendingHPChange(string participant, int change)
    {
        if (!pendingHPChanges.ContainsKey(participant))
        {
            pendingHPChanges[participant] = 0;
        }
        pendingHPChanges[participant] += change;
    }

    private void UpdateHPSummary(string participant, int change)
    {
        if (!currentLog.hp_summary.ContainsKey(participant))
        {
            currentLog.hp_summary[participant] = 0;
        }
        currentLog.hp_summary[participant] += change;
    }

    /// <summary>
    /// Save the current log to JSON (updates existing file)
    /// </summary>
    public void SaveLog()
    {
        if (currentLog == null) return;

        // Use persistent file path if available
        if (string.IsNullOrEmpty(currentLogFilePath))
        {
            Debug.LogWarning("CombatLogger: No log file path set");
            return;
        }

        string json = JsonUtility.ToJson(new CombatLogWrapper(currentLog), true);
        File.WriteAllText(currentLogFilePath, json);
    }

    /// <summary>
    /// Get the folder path for combat logs (Campaigns/{campaignName}/CombatLogs)
    /// </summary>
    private string GetCombatLogsFolder()
    {
        string campaignName = CampaignManager.Instance?.GetCurrentCampaign()?.campaignName;
        
        string path;
        if (string.IsNullOrEmpty(campaignName))
        {
            // Fallback to persistent data path if no campaign
            path = Path.Combine(Application.persistentDataPath, logFolder);
            Debug.Log($"CombatLogger: No campaign loaded, using fallback path: {path}");
        }
        else
        {
            // Use the same path pattern as other campaign data
            #if UNITY_EDITOR
                path = Path.Combine(Application.dataPath, "Campaigns", campaignName, logFolder);
            #else
                path = Path.Combine(Application.persistentDataPath, "Campaigns", campaignName, logFolder);
            #endif
        }
        
        return path;
    }

    /// <summary>
    /// Get the current log (for external access)
    /// </summary>
    public CombatLog GetCurrentLog() => currentLog;

    /// <summary>
    /// Check if currently logging
    /// </summary>
    public bool IsLogging() => isLogging;

    /// <summary>
    /// Get grid position from a token
    /// </summary>
    public static GridPosition GetTokenPosition(Token token)
    {
        if (token == null) return new GridPosition { gridX = -1, gridY = -1 };

        Tile tile = token.GetCurrentTile();
        if (tile != null)
        {
            return new GridPosition { gridX = tile.GridX, gridY = tile.GridY };
        }

        // Fallback to world position
        return new GridPosition
        {
            gridX = Mathf.RoundToInt(token.transform.position.x),
            gridY = Mathf.RoundToInt(token.transform.position.y)
        };
    }

    /// <summary>
    /// Get participant name (handles both characters and enemies)
    /// </summary>
    public static string GetParticipantName(Token token)
    {
        if (token == null) return "unknown";

        if (token.getCharacterType() == CharacterType.Enemy)
        {
            return token.getEnemyData()?.name ?? "enemy";
        }
        return token.getCharacterData()?.charName ?? "player";
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

// Data structures for JSON serialization
[Serializable]
public class CombatLog
{
    public string timestamp;
    public List<ParticipantInfo> participants;
    public Dictionary<string, int> hp_summary;
    public List<CombatAction> action_log;
}

[Serializable]
public class ParticipantInfo
{
    public string name;
    public int max_hp;
    public int ac;
}

[Serializable]
public class CombatAction
{
    public string type;           // "damage", "healing", "movement"
    public string source;         // Who performed the action
    public string target;         // Who received the action (damage/healing)
    public string actor;          // For movement - who moved
    public int hp_removed;        // For damage
    public int hp_restored;       // For healing
    public string description;    // Action description (e.g., "used a sword")
    public ActionPositions positions;
}

[Serializable]
public class ActionPositions
{
    public GridPosition source;
    public GridPosition target;
}

[Serializable]
public class GridPosition
{
    public int gridX;
    public int gridY;
}

// Wrapper for JsonUtility (doesn't support Dictionary directly)
[Serializable]
public class CombatLogWrapper
{
    public string timestamp;
    public List<ParticipantInfo> participants;
    public List<HPSummaryEntry> hp_summary;
    public List<CombatAction> action_log;

    public CombatLogWrapper(CombatLog log)
    {
        timestamp = log.timestamp;
        participants = log.participants;
        action_log = log.action_log;
        hp_summary = new List<HPSummaryEntry>();
        
        foreach (var kvp in log.hp_summary)
        {
            hp_summary.Add(new HPSummaryEntry { participant = kvp.Key, hp_change = kvp.Value });
        }
    }
}

[Serializable]
public class HPSummaryEntry
{
    public string participant;
    public int hp_change;
}

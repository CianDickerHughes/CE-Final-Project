using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

//Main manager for handling the DM AI Logic - receives observations from CombatManager and turns them into promots
//Then calls brain to process and handles response - if confidence is high enough it causes the enemy to get a boost
public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DMBrain dmBrain;

    [Header("Confidence Settings")]
    [Tooltip("Minimum confidence before the DM starts reacting (soft threshold)")]
    [Range(0f, 1f)]
    public float minimumConfidenceToAct = 0.55f;

    [Tooltip("Confidence required to lock in the class and trigger the power boost")]
    [Range(0f, 1f)]
    public float lockConfidenceThreshold = 0.80f;

    //Cumulative observation state
    private List<string> abilitiesUsed  = new List<string>();
    private List<string> spellsCast     = new List<string>();
    private List<string> weaponsUsed    = new List<string>();
    private int totalDamage   = 0;
    private int totalHealing  = 0;
    private int turnsObserved = 0;

    //Strategy state - handles the logic around the dm processing things and what class they belive the player to be
    private bool   classLocked    = false;
    private string lockedClass    = "";
    private bool   processingTurn = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Public API

    //Notifies the player turn has ended - causes AI to act by reading the log and sending an observation to the brain
    //Called by CombatManager at the end of the player's turn
    public async void NotifyPlayerTurnEnded()
    {
        if (classLocked || processingTurn) return;

        if (dmBrain == null)
        {
            Debug.LogWarning("[AIManager] DMBrain reference not assigned.");
            return;
        }

        if (!dmBrain.IsReady)
        {
            Debug.Log("[AIManager] DMBrain not ready yet — Ollama may still be loading.");
            return;
        }

        processingTurn = true;
        turnsObserved++;

        //Accumulate data from the saved log file
        ParseCombatLog();

        //Need at least 1 weapon/spell/ability observed before asking
        if (turnsObserved < 1)
        {
            processingTurn = false;
            return;
        }

        string observationJson = BuildObservationJson();
        Debug.Log($"[AIManager] Sending turn {turnsObserved} observation to DMBrain:\n{observationJson}");

        DMHypothesis result = await dmBrain.AnalyseAsync(observationJson);

        processingTurn = false;

        if (result != null)
            HandleHypothesis(result);
    }

    //Called when resetting the combat - clears all previously observed data and resets
    public void ResetForNewCombat()
    {
        abilitiesUsed.Clear();
        spellsCast.Clear();
        weaponsUsed.Clear();
        totalDamage    = 0;
        totalHealing   = 0;
        turnsObserved  = 0;
        classLocked    = false;
        lockedClass    = "";
        processingTurn = false;
        Debug.Log("[AIManager] Reset for new combat.");
    }

    // Log parsing — reads the structured JSON written by CombatLogger

    private void ParseCombatLog()
    {
        //CombatLogger saves to the campaign folder; DMFight has no campaign so it falls back to Application.persistentDataPath/CombatLogs/combat_log.json
        string logPath = FindCombatLogPath();

        if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
        {
            Debug.LogWarning($"[AIManager] combat_log.json not found. Tried: {logPath}");
            return;
        }

        string raw = File.ReadAllText(logPath);
        if (string.IsNullOrEmpty(raw)) return;

        //Re-scan the full log each time so totals are always accurate
        totalDamage  = 0;
        totalHealing = 0;

        try
        {
            CombatLogWrapper log = JsonUtility.FromJson<CombatLogWrapper>(raw);
            if (log?.action_log == null) return;

            foreach (var action in log.action_log)
            {
                if (action == null) continue;

                string desc = action.description ?? "";

                //Abilities used 
                if (action.type == "ability")
                {
                    //Description is "Used <AbilityName> - matches the format from training"
                    string abilityName = desc.StartsWith("Used ") ? desc.Substring(5).Trim() : desc.Trim();
                    if (!string.IsNullOrEmpty(abilityName) && !abilitiesUsed.Contains(abilityName))
                        abilitiesUsed.Add(abilityName);
                }

                //Damage actions - all forms of damage the player could use
                if (action.type == "damage")
                {
                    totalDamage += action.hp_removed;

                    // Weapon: "attacked with <weapon>"
                    if (desc.StartsWith("attacked with "))
                    {
                        string weapon = desc.Substring(14).Trim();
                        if (!string.IsNullOrEmpty(weapon) && !weaponsUsed.Contains(weapon))
                            weaponsUsed.Add(weapon);
                    }

                    // Spell: "cast <spell>"
                    if (desc.StartsWith("cast "))
                    {
                        string spell = desc.Substring(5).Trim();
                        if (!string.IsNullOrEmpty(spell) && !spellsCast.Contains(spell))
                            spellsCast.Add(spell);
                    }

                    // Ability used for damage: "Used <ability>"
                    if (desc.StartsWith("Used "))
                    {
                        string ability = desc.Substring(5).Trim();
                        if (!string.IsNullOrEmpty(ability) && !abilitiesUsed.Contains(ability))
                            abilitiesUsed.Add(ability);
                    }
                }

                //Healing actions
                if (action.type == "healing")
                {
                    totalHealing += action.hp_restored;

                    if (desc.StartsWith("cast "))
                    {
                        string spell = desc.Substring(5).Trim();
                        if (!string.IsNullOrEmpty(spell) && !spellsCast.Contains(spell))
                            spellsCast.Add(spell);
                    }

                    if (desc.StartsWith("Used "))
                    {
                        string ability = desc.Substring(5).Trim();
                        if (!string.IsNullOrEmpty(ability) && !abilitiesUsed.Contains(ability))
                            abilitiesUsed.Add(ability);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIManager] Failed to parse combat log: {e.Message}");
        }
    }

    private string FindCombatLogPath()
    {
        // Match the same fallback logic as CombatLogger.GetCombatLogsFolder()
        // In DMFight there's no campaign loaded, so we go straight to the fallback.
        return Path.Combine(Application.persistentDataPath, "CombatLogs", "combat_log.json");
    }

    //Observation builder - helps to build up a structured JSON to send the the AI of actions performed
    private string BuildObservationJson()
    {
        return "{\n" +
               $"  \"turns_observed\": {turnsObserved},\n" +
               $"  \"abilities_used\": {ToJsonArray(abilitiesUsed)},\n" +
               $"  \"spells_cast\": {ToJsonArray(spellsCast)},\n" +
               $"  \"weapons_observed\": {ToJsonArray(weaponsUsed)},\n" +
               $"  \"total_damage_dealt\": {totalDamage},\n" +
               $"  \"total_healing_done\": {totalHealing}\n" +
               "}";
    }

    private string ToJsonArray(List<string> items)
    {
        if (items == null || items.Count == 0) return "[]";
        var escaped = items.ConvertAll(i => $"\"{i.Replace("\"", "\\\"")}\"");
        return "[" + string.Join(", ", escaped) + "]";
    }

    // Hypothesis handling
    private void HandleHypothesis(DMHypothesis h)
    {
        if (h == null) return;

        Debug.Log($"[AIManager] Hypothesis: {h.ClassHypothesis} | " +
                  $"Confidence: {h.Confidence:P0} | Reasoning: {h.Reasoning}");

        if (h.Confidence < minimumConfidenceToAct)
        {
            Debug.Log("[AIManager] Confidence too low — DM still observing.");
            return;
        }

        if (h.Confidence >= lockConfidenceThreshold && !classLocked)
        {
            classLocked = true;
            lockedClass = h.ClassHypothesis;
            Debug.Log($"[AIManager] Class LOCKED: {lockedClass}");

            DMEnemyController dmController = FindDMController();
            if (dmController != null)
                dmController.OnClassIdentified(lockedClass);
            else
                Debug.LogWarning("[AIManager] Could not find DMEnemyController on DM token.");
        }
    }

    //Helper for finding the DMEnemyController in initiative order

    private DMEnemyController FindDMController()
    {
        if (CombatManager.Instance == null) return null;

        foreach (var participant in CombatManager.Instance.GetInitiativeOrder())
        {
            if (participant.token != null &&
                participant.token.getCharacterType() == CharacterType.Enemy)
            {
                DMEnemyController ctrl = participant.token.GetComponent<DMEnemyController>();
                if (ctrl != null) return ctrl;
            }
        }
        return null;
    }
}
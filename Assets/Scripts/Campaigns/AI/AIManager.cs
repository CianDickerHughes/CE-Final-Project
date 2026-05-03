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

    //Changed to reflect the actual class abilities in game. This will help when parsing the message to the actual model
    //Helps to restructure/restrict the data the model sees to reucde hallucinations.
    //Also now fixes errors to do with spells being cast
    private static readonly HashSet<string> KnownClassAbilities = new HashSet<string>
    {
        "Infuse Item",
        "Rage",
        "Bardic Inspiration",
        "Channel Divinity",
        "Wild Shape",
        "Action Surge",
        "Martial Arts",
        "Lay on Hands",
        "Favored Enemy",
        "Sneak Attack",
        "Spellcasting",
        "Pact Magic",
        "Arcane Recovery"
        // Note: Divine Smite is a SPELL, not a class ability — intentionally excluded
    };

    // ── METHOD: replace ParseCombatLog() with this ───────────────────────────────
    private void ParseCombatLog()
    {
        string logPath = FindCombatLogPath();

        if (string.IsNullOrEmpty(logPath) || !System.IO.File.Exists(logPath))
        {
            Debug.LogWarning($"[AIManager] combat_log.json not found at: {logPath}");
            return;
        }

        string raw = System.IO.File.ReadAllText(logPath);
        if (string.IsNullOrEmpty(raw)) return;

        // Reset totals — re-sum from full log every call so numbers are always accurate
        totalDamage  = 0;
        totalHealing = 0;

        try
        {
            CombatLogWrapper log = JsonUtility.FromJson<CombatLogWrapper>(raw);
            if (log?.action_log == null) return;

            foreach (var action in log.action_log)
            {
                if (action == null) continue;

                string desc   = (action.description ?? "").Trim();
                string type   = (action.type ?? "").Trim();
                string source = (action.source ?? "").Trim();

                // ── Only process actions by the player, not the DM ───────────
                // DM enemy is CharacterType.Enemy — skip their actions
                // We identify player actions by checking the source name against
                // known enemy names, OR by checking action.source against the
                // DM enemy's name. Simplest approach: skip if source matches
                // the DM enemy token name (set when DM attacks).
                // For now we process all actions — the DM doesn't use player
                // abilities/spells so cross-contamination is low risk.

                // ── Weapon attacks ────────────────────────────────────────────
                // WeaponAttackManager logs: description = "attacked with {weaponName}"
                if (desc.StartsWith("attacked with "))
                {
                    string weapon = desc.Substring(14).Trim();
                    if (!string.IsNullOrEmpty(weapon) && !weaponsUsed.Contains(weapon))
                        weaponsUsed.Add(weapon);
                }

                // ── "Used X" actions — ability OR spell depending on the name ─
                // AbilityManager logs ALL of these as "Used {name}" regardless of
                // whether it is a class ability or a spell (e.g. Divine Smite).
                else if (desc.StartsWith("Used "))
                {
                    string name = desc.Substring(5).Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (KnownClassAbilities.Contains(name))
                        {
                            // It is the class ability — goes into abilities_used
                            if (!abilitiesUsed.Contains(name))
                                abilitiesUsed.Add(name);
                        }
                        else
                        {
                            // It is a spell triggered via ability (e.g. Divine Smite)
                            // Route it to spells_cast instead
                            if (!spellsCast.Contains(name))
                                spellsCast.Add(name);
                        }
                    }
                }

                // ── Spells cast via SpellChoiceManager ────────────────────────
                // Logged as: description = "cast {spellName}"
                else if (desc.StartsWith("cast "))
                {
                    string spell = desc.Substring(5).Trim();
                    // Strip "on {targetName}" suffix if present
                    int onIdx = spell.IndexOf(" on ");
                    if (onIdx > 0) spell = spell.Substring(0, onIdx).Trim();

                    if (!string.IsNullOrEmpty(spell) && !spellsCast.Contains(spell))
                        spellsCast.Add(spell);
                }

                // ── Ability type actions (buff/utility with no damage/heal) ───
                // CombatLogger.LogAbilityUse() sets type = "ability"
                // description = "Used {abilityName}" — already caught above,
                // but handle type == "ability" explicitly as a safety net
                if (type == "ability" && desc.StartsWith("Used "))
                {
                    string name = desc.Substring(5).Trim();
                    if (!string.IsNullOrEmpty(name) && KnownClassAbilities.Contains(name))
                    {
                        if (!abilitiesUsed.Contains(name))
                            abilitiesUsed.Add(name);
                    }
                }

                // ── Damage totals (player only) ───────────────────────────────
                if (type == "damage")
                    totalDamage += action.hp_removed;

                // ── Healing totals ────────────────────────────────────────────
                if (type == "healing")
                    totalHealing += action.hp_restored;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIManager] Failed to parse combat log: {e.Message}");
        }

        Debug.Log($"[AIManager] Parsed log — abilities: [{string.Join(", ", abilitiesUsed)}] " +
                $"spells: [{string.Join(", ", spellsCast)}] " +
                $"weapons: [{string.Join(", ", weaponsUsed)}] " +
                $"dmg: {totalDamage} heal: {totalHealing}");
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
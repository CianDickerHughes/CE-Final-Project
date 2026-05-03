using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Attached to the main DM enemy token prefab in the fight scene
//Handles DM enemy behavior during combat — movement, attacking, and the "figured out" 
//If it figures out the players class it triggers a UI view and gets power boost
//Could update to just kill instantly
public class DMEnemyController : MonoBehaviour
{
    //UI Elements and settings for the message and stats for the DM enemy
    [Header("Movement")]
    [Tooltip("How many tiles the DM moves per turn")]
    public int movesPerTurn = 3;

    [Tooltip("Seconds between each tile step so movement is visible")]
    public float moveDelay = 0.25f;

    [Header("Attack")]
    [Tooltip("Base damage dice string from DungeonMaster.json — e.g. '3d8'")]
    public string baseDamageDice = "3d8";

    [Tooltip("Flat bonus added to damage roll")]
    public int damageBonus = 5;

    [Header("Figured Out — Power Boost")]
    [Tooltip("Damage multiplier applied once the AI has identified the player's class")]
    public float figuredOutDamageMultiplier = 2.5f;

    [Header("UI — Figured Out Message")]
    [Tooltip("Optional UI panel to show when DM identifies the player's class")]
    [SerializeField] private GameObject figuredOutPanel;
    [Tooltip("Text field inside that panel")]
    [SerializeField] private TextMeshProUGUI figuredOutText;
    [Tooltip("Seconds to show the message before hiding it")]
    public float messageDisplayTime = 4f;

    //State
    private Token       token;
    private GridManager gridManager;
    private bool        figuredOut      = false;
    private string      identifiedClass = "";

    void Awake()
    {
        token = GetComponent<Token>();
    }

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            Debug.LogError("[DMEnemyController] Could not find GridManager in scene!");

        // Hide the figured-out panel at start
        if (figuredOutPanel != null)
            figuredOutPanel.SetActive(false);
    }

    //Public API — called by AIManager when class is identified

    //Main method called by AIManager once the class is identified correctly
    public void OnClassIdentified(string playerClass)
    {
        if (figuredOut)
        {
            return;
        }

        figuredOut      = true;
        identifiedClass = playerClass;

        Debug.Log($"[DMEnemyController] Class identified: {playerClass} — activating power boost!");

        // Show message to the player
        StartCoroutine(ShowFiguredOutMessage(playerClass));
    }

    //Turn execution — called by CombatManager when it's DM's turn
    public void ExecuteTurn()
    {
        if (token == null || gridManager == null)
        {
            Debug.LogWarning("[DMEnemyController] Missing references — ending turn.");
            EndTurn();
            return;
        }

        Token playerToken = GetPlayerToken();
        if (playerToken == null)
        {
            Debug.LogWarning("[DMEnemyController] No player token found — ending turn.");
            EndTurn();
            return;
        }

        StartCoroutine(TurnCoroutine(playerToken));
    }

    //The coroutine handles the enemys turn
    //Movement, attacking and other waiting aspects
    private IEnumerator TurnCoroutine(Token playerToken)
    {
        Tile myTile     = token.GetCurrentTile();
        Tile playerTile = playerToken.GetCurrentTile();

        if (myTile == null || playerTile == null)
        {
            EndTurn();
            yield break;
        }

        //1. Move toward player
        int distance = ManhattanDistance(myTile, playerTile);
        int stepsTaken = 0;

        while (stepsTaken < movesPerTurn && distance > 1)
        {
            Tile nextTile = StepToward(myTile, playerTile);

            if (nextTile == null || !nextTile.IsWalkable() || IsTileOccupied(nextTile))
                break;

            token.MoveToTile(nextTile);
            myTile   = nextTile;
            distance = ManhattanDistance(myTile, playerTile);
            stepsTaken++;

            yield return new WaitForSeconds(moveDelay);
        }

        //Small pause after moving so the player can register the position
        yield return new WaitForSeconds(0.2f);

        //2. Attack if adjacent
        if (distance <= 1)
        {
            PerformAttack(playerToken);
        }

        yield return new WaitForSeconds(0.3f);
        EndTurn();
    }

    //Attack logic - rolling damage and applying boosts etc, then applying it to the player
    private void PerformAttack(Token target)
    {
        if (CombatManager.Instance == null) return;

        //Find the target's index in the initiative order
        int targetIndex = -1;
        var initiativeOrder = CombatManager.Instance.GetInitiativeOrder();
        for (int i = 0; i < initiativeOrder.Count; i++)
        {
            if (initiativeOrder[i].token == target)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
        {
            Debug.LogWarning("[DMEnemyController] Could not find target in initiative order.");
            return;
        }

        //Roll damage
        int damage = RollDamage();

        //Apply power boost if class has been identified
        if (figuredOut)
        {
            damage = Mathf.RoundToInt(damage * figuredOutDamageMultiplier);
            Debug.Log($"[DMEnemyController] Power boost active! Damage: {damage}");
        }

        Debug.Log($"[DMEnemyController] DM attacks for {damage} damage!");

        //Apply to CombatManager — this handles HP, death, UI update, logging
        CombatManager.Instance.ApplyDamage(targetIndex, damage);

        //Log the attack to the combat log
        if (CombatLogger.Instance != null)
        {
            string dmName     = token.getEnemyData()?.name ?? "Dungeon Master";
            string targetName = CombatLogger.GetParticipantName(target);
            GridPosition dmPos     = CombatLogger.GetTokenPosition(token);
            GridPosition targetPos = CombatLogger.GetTokenPosition(target);

            CombatLogger.Instance.LogDamage(
                dmName,
                targetName,
                damage,
                figuredOut
                    ? $"attacks with {token.getEnemyData()?.weapon ?? "weapon"} [EMPOWERED — knows player is {identifiedClass}]"
                    : $"attacks with {token.getEnemyData()?.weapon ?? "weapon"}",
                dmPos,
                targetPos
            );
        }
    }

    private int RollDamage()
    {
        //Parse the dice string e.g. "3d8"
        Match m = Regex.Match(baseDamageDice, @"(\d+)d(\d+)");
        if (!m.Success) return damageBonus;

        int numDice  = int.Parse(m.Groups[1].Value);
        int dieSides = int.Parse(m.Groups[2].Value);

        int total = damageBonus;
        for (int i = 0; i < numDice; i++)
            total += Random.Range(1, dieSides + 1);

        return total;
    }

    // Figured Out message

    private IEnumerator ShowFiguredOutMessage(string playerClass)
    {
        if (figuredOutPanel != null)
        {
            if (figuredOutText != null)
                figuredOutText.text = $"The Dungeon Master has figured you out!\nYou fight as a {playerClass}...\nThere is no escape.";

            figuredOutPanel.SetActive(true);
            yield return new WaitForSeconds(messageDisplayTime);
            figuredOutPanel.SetActive(false);
        }
        else
        {
            // Fallback if no UI panel assigned — just log it
            Debug.Log($"[DMEnemyController] *** The Dungeon Master has identified the player as a {playerClass}! ***");
            yield return null;
        }
    }

    //Helpers - things like movement, distance calculation, checking surroundings etc
    private Tile StepToward(Tile from, Tile target)
    {
        int dx = target.GridX - from.GridX;
        int dy = target.GridY - from.GridY;

        int nextX = from.GridX;
        int nextY = from.GridY;

        //Move one step in the dominant direction
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            nextX += (int)Mathf.Sign(dx);
        else
            nextY += (int)Mathf.Sign(dy);

        return gridManager.GetTileAtPosition(new Vector2(nextX, nextY));
    }

    private bool IsTileOccupied(Tile tile)
    {
        if (TokenManager.Instance == null) return false;
        foreach (Token t in TokenManager.Instance.GetSpawnedTokens())
        {
            if (t == token) continue;
            if (t.GetCurrentTile() == tile) return true;
        }
        return false;
    }

    private int ManhattanDistance(Tile a, Tile b)
    {
        return Mathf.Abs(a.GridX - b.GridX) + Mathf.Abs(a.GridY - b.GridY);
    }

    private Token GetPlayerToken()
    {
        if (CombatManager.Instance == null) return null;
        foreach (var p in CombatManager.Instance.GetInitiativeOrder())
        {
            if (p.token != null && p.token.getCharacterType() == CharacterType.Player)
                return p.token;
        }
        return null;
    }

    private void EndTurn()
    {
        Debug.Log("[DMEnemyController] DM turn complete.");
        CombatManager.Instance?.SetCurrentParticipantActed(true);
        CombatManager.Instance?.AdvanceTurn();
    }
}
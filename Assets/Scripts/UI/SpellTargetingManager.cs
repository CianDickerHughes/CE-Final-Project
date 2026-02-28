using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages spell targeting after a spell is selected.
/// Handles clicking on tokens to cast spells based on spell type.
/// - Damage: Target enemies
/// - Healing: Target self or allies
/// - Utility: Target self
/// </summary>
public class SpellTargetingManager : MonoBehaviour
{
    [Header("UI Feedback")]
    [SerializeField] private GameObject targetingIndicator; // Optional: UI to show targeting mode is active
    [SerializeField] private TextMeshProUGUI targetingText; // Shows "Select a target..." message

    [Header("Targeting Settings")]
    [SerializeField] private Color validTargetColor = new Color(0.5f, 1f, 0.5f, 1f); // Green tint for valid targets
    [SerializeField] private Color invalidTargetColor = new Color(1f, 0.5f, 0.5f, 1f); // Red tint for invalid targets

    // Current state
    private bool isTargeting = false;
    private SpellDefinition currentSpell;
    private CharacterData caster;
    private Token casterToken;

    // Singleton
    public static SpellTargetingManager Instance { get; private set; }

    // Events
    public event System.Action<SpellDefinition, Token, int> OnSpellCast; // spell, target, result

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Subscribe to spell selection events
        if (SpellChoiceManager.Instance != null)
        {
            SpellChoiceManager.Instance.OnSpellCast += StartTargeting;
        }

        // Hide targeting UI initially
        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(false);
        }
    }

    void Update()
    {
        if (!isTargeting) return;

        // Check for right-click to cancel targeting
        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
            return;
        }

        // Check for left-click on a token
        if (Input.GetMouseButtonDown(0))
        {
            CheckForTargetClick();
        }
    }

    /// <summary>
    /// Start targeting mode with the selected spell
    /// </summary>
    public void StartTargeting(SpellDefinition spell)
    {
        if (spell == null)
        {
            Debug.LogError("SpellTargetingManager: Cannot start targeting with null spell");
            return;
        }

        currentSpell = spell;
        isTargeting = true;

        // Get the caster info
        caster = GetCaster();
        casterToken = GetCasterToken();

        // Update UI
        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(true);
        }

        if (targetingText != null)
        {
            string targetTypeText = GetTargetTypeText(spell.spellType);
            targetingText.text = $"Casting {FormatSpellName(spell.spellName.ToString())}\n{targetTypeText}";
        }

        Debug.Log($"SpellTargetingManager: Targeting mode started for {spell.spellName} ({spell.spellType})");
    }

    /// <summary>
    /// Cancel the current targeting
    /// </summary>
    public void CancelTargeting()
    {
        isTargeting = false;
        currentSpell = null;
        caster = null;
        casterToken = null;

        if (targetingIndicator != null)
        {
            targetingIndicator.SetActive(false);
        }

        Debug.Log("SpellTargetingManager: Targeting cancelled");
    }

    /// <summary>
    /// Check if user clicked on a valid target
    /// </summary>
    private void CheckForTargetClick()
    {
        // Raycast to find what was clicked
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Token targetToken = hit.collider.GetComponent<Token>();
            if (targetToken != null)
            {
                TryTargetToken(targetToken);
                return;
            }
        }

        // Clicked on nothing - optionally cancel or just ignore
        Debug.Log("SpellTargetingManager: Clicked on non-target area");
    }

    /// <summary>
    /// Try to cast the spell on the target token
    /// </summary>
    private void TryTargetToken(Token targetToken)
    {
        if (!IsValidTarget(targetToken))
        {
            Debug.Log($"SpellTargetingManager: Invalid target for {currentSpell.spellType} spell");
            // Could show feedback to user here
            return;
        }

        // Cast the spell!
        CastSpell(targetToken);
    }

    /// <summary>
    /// Check if the token is a valid target for the current spell type
    /// </summary>
    private bool IsValidTarget(Token targetToken)
    {
        if (targetToken == null || currentSpell == null) return false;

        CharacterType targetType = targetToken.getCharacterType();

        switch (currentSpell.spellType)
        {
            case SpellType.Damage:
                // Damage spells target enemies (or anyone not the caster for PvP)
                // For now: cannot target self
                return targetToken != casterToken;

            case SpellType.Healing:
                // Healing spells can target self or allies (non-enemies)
                return targetType != CharacterType.Enemy;

            case SpellType.Utility:
                // Utility spells can target self or allies
                return targetType != CharacterType.Enemy;

            default:
                return true;
        }
    }

    /// <summary>
    /// Execute the spell on the target
    /// </summary>
    private void CastSpell(Token targetToken)
    {
        if (currentSpell == null || targetToken == null) return;

        // Roll the dice
        int rollResult = RollSpellDice(currentSpell);

        // Get target's participant index in combat
        int targetIndex = GetParticipantIndex(targetToken);

        // Apply effect based on spell type
        switch (currentSpell.spellType)
        {
            case SpellType.Damage:
                ApplyDamageSpell(targetToken, targetIndex, rollResult);
                break;

            case SpellType.Healing:
                ApplyHealingSpell(targetToken, targetIndex, rollResult);
                break;

            case SpellType.Utility:
                ApplyUtilitySpell(targetToken, rollResult);
                break;
        }

        // Broadcast to chat
        BroadcastSpellCast(targetToken, rollResult);

        // Fire event
        OnSpellCast?.Invoke(currentSpell, targetToken, rollResult);

        // End targeting mode
        CancelTargeting();
    }

    /// <summary>
    /// Roll dice for the spell
    /// </summary>
    private int RollSpellDice(SpellDefinition spell)
    {
        if (spell.diceCount <= 0 || spell.diceSize <= 0)
        {
            return 0; // No dice to roll (utility spell)
        }

        int total = 0;
        for (int i = 0; i < spell.diceCount; i++)
        {
            total += Random.Range(1, spell.diceSize + 1);
        }

        Debug.Log($"SpellTargetingManager: Rolled {spell.diceCount}d{spell.diceSize} = {total}");
        return total;
    }

    /// <summary>
    /// Apply damage to the target
    /// </summary>
    private void ApplyDamageSpell(Token target, int participantIndex, int damage)
    {
        if (CombatManager.Instance != null && participantIndex >= 0)
        {
            CombatManager.Instance.ApplyDamage(participantIndex, damage);
        }

        string targetName = GetTokenName(target);
        Debug.Log($"SpellTargetingManager: {currentSpell.spellName} dealt {damage} damage to {targetName}");
    }

    /// <summary>
    /// Apply healing to the target
    /// </summary>
    private void ApplyHealingSpell(Token target, int participantIndex, int healing)
    {
        if (CombatManager.Instance != null && participantIndex >= 0)
        {
            CombatManager.Instance.ApplyHealing(participantIndex, healing);
        }

        string targetName = GetTokenName(target);
        Debug.Log($"SpellTargetingManager: {currentSpell.spellName} healed {targetName} for {healing}");
    }

    /// <summary>
    /// Apply utility spell effect
    /// </summary>
    private void ApplyUtilitySpell(Token target, int rollResult)
    {
        // Utility spells might not have direct HP effects
        // This could trigger buffs, status effects, etc in the future
        string targetName = GetTokenName(target);
        Debug.Log($"SpellTargetingManager: {currentSpell.spellName} cast on {targetName}");
    }

    /// <summary>
    /// Broadcast the spell cast to chat
    /// </summary>
    private void BroadcastSpellCast(Token target, int rollResult)
    {
        if (ChatNetwork.Instance == null) return;

        string casterName = caster?.charName ?? "Unknown";
        string targetName = GetTokenName(target);
        string spellName = FormatSpellName(currentSpell.spellName.ToString());
        string diceExpr = currentSpell.diceCount > 0 ? $"{currentSpell.diceCount}d{currentSpell.diceSize}" : "";

        string message;
        switch (currentSpell.spellType)
        {
            case SpellType.Damage:
                message = $"cast {spellName} on {targetName}! ({diceExpr}: {rollResult} damage)";
                break;
            case SpellType.Healing:
                message = $"cast {spellName} on {targetName}! ({diceExpr}: {rollResult} healing)";
                break;
            default:
                message = $"cast {spellName} on {targetName}!";
                break;
        }

        ChatNetwork.Instance.SendMessage(message, casterName);
    }

    /// <summary>
    /// Get the participant index for a token in combat
    /// </summary>
    private int GetParticipantIndex(Token token)
    {
        if (CombatManager.Instance == null) return -1;

        var initiativeOrder = CombatManager.Instance.GetInitiativeOrder();
        if (initiativeOrder == null) return -1;

        for (int i = 0; i < initiativeOrder.Count; i++)
        {
            if (initiativeOrder[i].token == token)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the current caster's character data
    /// </summary>
    private CharacterData GetCaster()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.GetCombatState() == CombatState.Active)
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            if (currentParticipant.token != null)
            {
                return currentParticipant.token.getCharacterData();
            }
        }

        if (PlayerAssignmentHelper.Instance != null)
        {
            return PlayerAssignmentHelper.Instance.GetMyCharacter();
        }

        return null;
    }

    /// <summary>
    /// Get the current caster's token
    /// </summary>
    private Token GetCasterToken()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.GetCombatState() == CombatState.Active)
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            return currentParticipant.token;
        }

        return null;
    }

    /// <summary>
    /// Get the name of a token
    /// </summary>
    private string GetTokenName(Token token)
    {
        if (token == null) return "Unknown";

        if (token.getCharacterType() == CharacterType.Enemy)
        {
            return token.getEnemyData()?.name ?? "Enemy";
        }
        else
        {
            return token.getCharacterData()?.charName ?? "Character";
        }
    }

    /// <summary>
    /// Get targeting text based on spell type
    /// </summary>
    private string GetTargetTypeText(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.Damage:
                return "Click on a target to attack";
            case SpellType.Healing:
                return "Click on yourself or an ally to heal";
            case SpellType.Utility:
                return "Click on a target to cast";
            default:
                return "Select a target";
        }
    }

    /// <summary>
    /// Check if currently in targeting mode
    /// </summary>
    public bool IsTargeting()
    {
        return isTargeting;
    }

    /// <summary>
    /// Get the current spell being targeted
    /// </summary>
    public SpellDefinition GetCurrentSpell()
    {
        return currentSpell;
    }

    /// <summary>
    /// Format spell name from enum
    /// </summary>
    private string FormatSpellName(string enumName)
    {
        if (string.IsNullOrEmpty(enumName)) return "";

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < enumName.Length; i++)
        {
            if (i > 0 && char.IsUpper(enumName[i]))
            {
                result.Append(' ');
            }
            result.Append(enumName[i]);
        }
        return result.ToString();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (SpellChoiceManager.Instance != null)
        {
            SpellChoiceManager.Instance.OnSpellCast -= StartTargeting;
        }
    }
}

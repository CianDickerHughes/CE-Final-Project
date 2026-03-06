using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//Manages the ability button UI and ability usage during gameplay.
//Each character has one class ability. Click the button to activate targeting,
//select a valid target, and the effect is applied (similar to spell flow but simpler)
public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Button abilityButton;
    [SerializeField] private TextMeshProUGUI abilityButtonText;
    [Tooltip("Optional panel shown during targeting mode")]
    [SerializeField] private GameObject abilityTargetingPanel;
    [SerializeField] private TextMeshProUGUI targetingInstructionText;

    [Header("Ability State")]
    private AbilityData currentAbility;
    private CharacterData currentCharacter;
    private bool isTargeting = false;
    private Dictionary<string, int> abilityUsesRemaining = new Dictionary<string, int>();

    // Event for when ability is used (other systems can subscribe)
    public event System.Action<AbilityData, Token> OnAbilityUsed;

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
        // Wire up ability button
        if (abilityButton != null)
        {
            abilityButton.onClick.AddListener(OnAbilityButtonClicked);
        }

        // Hide targeting panel initially
        if (abilityTargetingPanel != null)
        {
            abilityTargetingPanel.SetActive(false);
        }

        // Initial UI update
        UpdateAbilityUI();
    }

    void Update()
    {
        // Handle targeting mode clicks
        if (isTargeting && Input.GetMouseButtonDown(0))
        {
            HandleTargetClick();
        }

        // Cancel targeting with right-click or Escape
        if (isTargeting && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CancelTargeting();
        }
    }

    /// <summary>
    /// Updates the ability button to show the current player's class ability.
    /// Called when turns change or when combat starts.
    /// </summary>
    public void UpdateAbilityUI()
    {
        CharacterData character = GetCurrentCharacter();
        
        if (character == null)
        {
            SetButtonState("No Ability", false);
            return;
        }

        currentCharacter = character;
        
        // Get the single ability for this character's class
        List<AbilityData> abilities = AbilityDatabase.GetAbilitiesForClass(character.charClass);
        
        if (abilities == null || abilities.Count == 0)
        {
            SetButtonState("No Ability", false);
            return;
        }

        currentAbility = abilities[0]; // Each class has one ability
        
        // Check if ability has uses remaining
        int usesLeft = GetUsesRemaining(currentAbility.abilityName);
        bool canUse = usesLeft != 0; // -1 means unlimited
        
        // Format button text: "Lay on Hands (1/1)" or just "Sneak Attack" if unlimited
        string buttonText = currentAbility.abilityName;
        if (currentAbility.usesPerRest > 0)
        {
            buttonText += $" ({usesLeft}/{currentAbility.usesPerRest})";
        }
        
        SetButtonState(buttonText, canUse);

        Debug.Log($"AbilityManager: Updated UI for {character.charName} ({character.charClass}) - Ability: {currentAbility.abilityName}");
    }

    private void SetButtonState(string text, bool interactable)
    {
        if (abilityButtonText != null)
        {
            abilityButtonText.text = text;
        }
        
        if (abilityButton != null)
        {
            abilityButton.interactable = interactable;
        }
    }

    private void OnAbilityButtonClicked()
    {
        // If already targeting, clicking the button cancels targeting
        if (isTargeting)
        {
            CancelTargeting();
            return;
        }

        if (currentAbility == null || currentCharacter == null)
        {
            Debug.LogWarning("AbilityManager: No ability or character available");
            return;
        }

        // Check if it's this player's turn in combat
        if (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive())
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            if (currentParticipant.token != null)
            {
                CharacterData turnCharacter = currentParticipant.token.getCharacterData();
                if (turnCharacter == null || turnCharacter.id != currentCharacter.id)
                {
                    Debug.Log("AbilityManager: Not your turn!");
                    return;
                }
            }
        }

        // Check uses remaining
        int usesLeft = GetUsesRemaining(currentAbility.abilityName);
        if (usesLeft == 0)
        {
            Debug.Log($"AbilityManager: {currentAbility.abilityName} has no uses remaining");
            return;
        }

        // If ability requires a target, enter targeting mode
        // Otherwise, execute immediately (self-targeted abilities)
        if (currentAbility.requiresTarget)
        {
            StartTargeting();
        }
        else
        {
            // Self-targeted abilities (Rage, Wild Shape, etc.) activate immediately
            ExecuteAbility(null);
        }
    }

    private void StartTargeting()
    {
        isTargeting = true;
        
        // Show targeting panel if assigned
        if (abilityTargetingPanel != null)
        {
            abilityTargetingPanel.SetActive(true);
        }
        
        // Show targeting instruction based on target type
        if (targetingInstructionText != null)
        {
            targetingInstructionText.text = GetTargetingInstruction();
        }

        // Update button to show "Cancel" or similar
        if (abilityButtonText != null)
        {
            abilityButtonText.text = "Cancel";
        }

        Debug.Log($"AbilityManager: Select a target for {currentAbility.abilityName}");
    }

    private string GetTargetingInstruction()
    {
        switch (currentAbility.targetType)
        {
            case "Ally":
                return $"Select an ally to use {currentAbility.abilityName}";
            case "Enemy":
                return $"Select an enemy to use {currentAbility.abilityName}";
            case "Self":
                return "Click to confirm";
            case "Any":
                return $"Select any target for {currentAbility.abilityName}";
            default:
                return "Click on a target";
        }
    }

    private void HandleTargetClick()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Token targetToken = hit.collider.GetComponent<Token>();
            if (targetToken != null && IsValidTarget(targetToken))
            {
                ExecuteAbility(targetToken);
                CancelTargeting();
            }
        }
    }

    private bool IsValidTarget(Token target)
    {
        if (currentAbility == null) return false;

        CharacterType targetType = target.getCharacterType();
        
        switch (currentAbility.targetType)
        {
            case "Ally":
                return targetType == CharacterType.Player;
            case "Enemy":
                return targetType == CharacterType.Enemy;
            case "Self":
                // Check if this is the current character's token
                CharacterData targetData = target.getCharacterData();
                return targetData != null && targetData.id == currentCharacter.id;
            case "Any":
                return true;
            default:
                return false;
        }
    }

    private void CancelTargeting()
    {
        isTargeting = false;
        
        if (abilityTargetingPanel != null)
        {
            abilityTargetingPanel.SetActive(false);
        }

        // Restore the button text to show the ability name
        UpdateAbilityUI();
    }

    private void ExecuteAbility(Token target)
    {
        if (currentAbility == null) return;

        Debug.Log($"AbilityManager: Executing {currentAbility.abilityName}");

        // Calculate effect
        int effectValue = CalculateAbilityEffect();

        // Apply effect based on ability type
        switch (currentAbility.abilityType)
        {
            case AbilityType.Healing:
                ApplyHealingAbility(target, effectValue);
                break;
            case AbilityType.Damage:
                ApplyDamageAbility(target, effectValue);
                break;
            case AbilityType.Buff:
                ApplyBuffAbility(target);
                break;
            case AbilityType.Utility:
                ApplyUtilityAbility();
                break;
        }

        // Consume a use if limited
        if (currentAbility.usesPerRest > 0)
        {
            ConsumeAbilityUse(currentAbility.abilityName);
        }

        // Fire event
        OnAbilityUsed?.Invoke(currentAbility, target);

        // Update UI
        UpdateAbilityUI();
    }

    private int CalculateAbilityEffect()
    {
        int total = currentAbility.flatBonus;
        
        if (!string.IsNullOrEmpty(currentAbility.diceRoll))
        {
            total += RollDice(currentAbility.diceRoll);
        }
        
        return total;
    }

    private int RollDice(string diceNotation)
    {
        // Parse dice notation like "2d6"
        string[] parts = diceNotation.ToLower().Split('d');
        if (parts.Length != 2) return 0;
        
        if (!int.TryParse(parts[0], out int numDice)) numDice = 1;
        if (!int.TryParse(parts[1], out int sides)) return 0;
        
        int total = 0;
        for (int i = 0; i < numDice; i++)
        {
            total += UnityEngine.Random.Range(1, sides + 1);
        }
        
        Debug.Log($"AbilityManager: Rolled {diceNotation} = {total}");
        return total;
    }

    private void ApplyHealingAbility(Token target, int amount)
    {
        if (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive())
        {
            int targetIndex = GetParticipantIndex(target);
            if (targetIndex >= 0)
            {
                CombatManager.Instance.ApplyHealing(targetIndex, amount);
            }
        }
        
        string targetName = target != null ? GetTokenName(target) : "self";
        Debug.Log($"AbilityManager: {currentAbility.abilityName} healed {targetName} for {amount} HP");
    }

    private void ApplyDamageAbility(Token target, int amount)
    {
        if (target == null) return;
        
        if (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive())
        {
            int targetIndex = GetParticipantIndex(target);
            if (targetIndex >= 0)
            {
                CombatManager.Instance.ApplyDamage(targetIndex, amount);
            }
        }
        
        Debug.Log($"AbilityManager: {currentAbility.abilityName} dealt {amount} damage to {GetTokenName(target)}");
    }

    private void ApplyBuffAbility(Token target)
    {
        // Buff implementation - could add status effects system later
        string targetName = target != null ? GetTokenName(target) : currentCharacter.charName;
        Debug.Log($"AbilityManager: {currentAbility.abilityName} applied to {targetName}");
        
        // For now, just log the buff. You could expand this with a status effects system.
    }

    private void ApplyUtilityAbility()
    {
        Debug.Log($"AbilityManager: {currentAbility.abilityName} activated");
        
        // Utility abilities have varied effects - implement specific logic as needed
    }

    private int GetParticipantIndex(Token token)
    {
        if (CombatManager.Instance == null || token == null) return -1;
        
        var initiativeOrder = CombatManager.Instance.GetInitiativeOrder();
        for (int i = 0; i < initiativeOrder.Count; i++)
        {
            if (initiativeOrder[i].token == token)
            {
                return i;
            }
        }
        return -1;
    }

    private string GetTokenName(Token token)
    {
        if (token == null) return "Unknown";
        
        if (token.getCharacterType() == CharacterType.Enemy)
        {
            return token.getEnemyData()?.name ?? "Enemy";
        }
        return token.getCharacterData()?.charName ?? "Character";
    }

    private int GetUsesRemaining(string abilityName)
    {
        AbilityData ability = AbilityDatabase.GetAbility(abilityName);

        if (ability == null) 
        {
            return 0;
        }
        
        if (ability.usesPerRest < 0)
        {
            return -1; // Unlimited
        }
        
        if (!abilityUsesRemaining.ContainsKey(abilityName))
        {
            abilityUsesRemaining[abilityName] = ability.usesPerRest;
        }
        
        return abilityUsesRemaining[abilityName];
    }

    private void ConsumeAbilityUse(string abilityName)
    {
        if (abilityUsesRemaining.ContainsKey(abilityName) && abilityUsesRemaining[abilityName] > 0)
        {
            abilityUsesRemaining[abilityName]--;
        }
    }

    /// <summary>
    /// Reset ability uses (call on rest)
    /// </summary>
    public void ResetAbilityUses()
    {
        abilityUsesRemaining.Clear();
        UpdateAbilityUI();
    }

    /// <summary>
    /// Get the current character (follows same pattern as SpellChoiceManager)
    /// </summary>
    private CharacterData GetCurrentCharacter()
    {
        // First priority: Check for current turn in combat
        if (CombatManager.Instance != null && CombatManager.Instance.GetCombatState() == CombatState.Active)
        {
            var currentParticipant = CombatManager.Instance.GetCurrentTurnParticipant();
            if (currentParticipant.token != null)
            {
                CharacterData charData = currentParticipant.token.getCharacterData();
                if (charData != null)
                {
                    return charData;
                }
            }
        }

        // Second priority: Check for a selected token
        if (TokenManager.Instance != null)
        {
            Token selectedToken = TokenManager.Instance.GetSelectedToken();
            if (selectedToken != null)
            {
                CharacterData charData = selectedToken.getCharacterData();
                if (charData != null)
                {
                    return charData;
                }
            }
        }

        // Third priority: Try PlayerAssignmentHelper
        if (PlayerAssignmentHelper.Instance != null)
        {
            CharacterData character = PlayerAssignmentHelper.Instance.GetMyCharacter();
            if (character != null)
            {
                return character;
            }
        }

        return null;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
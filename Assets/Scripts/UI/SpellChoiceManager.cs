using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the spell choice panel UI.
/// Opens when the SpellAttack button is clicked and displays available spells
/// based on the player's character class.
/// </summary>
public class SpellChoiceManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject spellChoicePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI noSpellsText;

    [Header("Spell List")]
    [SerializeField] private Transform contentParent; // The Content object inside ScrollView
    [SerializeField] private GameObject spellItemPrefab; // SpellItem prefab

    [Header("Spell Attack Button")]
    [SerializeField] private Button spellAttackButton; // The SpellAttack button that opens this panel

    // Track instantiated spell items for cleanup
    private List<GameObject> spawnedSpellItems = new List<GameObject>();

    // Currently selected spell (if any)
    private SpellDefinition selectedSpell;

    // Event for when a spell is cast
    public event System.Action<SpellDefinition> OnSpellCast;

    // Singleton for easy access
    public static SpellChoiceManager Instance { get; private set; }

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
        // Wire up the SpellAttack button to open the panel
        if (spellAttackButton != null)
        {
            spellAttackButton.onClick.AddListener(OpenSpellPanel);
        }

        // Wire up close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSpellPanel);
        }

        // Hide panel on start
        if (spellChoicePanel != null)
        {
            spellChoicePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the spell panel and populates it with spells for the player's class
    /// </summary>
    public void OpenSpellPanel()
    {
        if (spellChoicePanel == null)
        {
            Debug.LogError("SpellChoiceManager: SpellChoicePanel is not assigned!");
            return;
        }

        // Get the player's character
        CharacterData playerCharacter = GetPlayerCharacter();
        
        if (playerCharacter == null)
        {
            Debug.LogWarning("SpellChoiceManager: No player character found.");
            ShowNoSpellsMessage("No character assigned.");
            spellChoicePanel.SetActive(true);
            return;
        }

        string charClass = playerCharacter.charClass;
        
        if (string.IsNullOrEmpty(charClass))
        {
            Debug.LogWarning("SpellChoiceManager: Player character has no class.");
            ShowNoSpellsMessage("Character has no class.");
            spellChoicePanel.SetActive(true);
            return;
        }

        // Get spells for this class
        // Using max spell level based on character level (simplified: level/2, minimum 1)
        int maxSpellLevel = Mathf.Max(1, playerCharacter.level / 2);
        List<SpellDefinition> availableSpells = SpellDatabase.GetAvailableSpells(charClass, maxSpellLevel);

        // Clear previous spell items
        ClearSpellItems();

        if (availableSpells == null || availableSpells.Count == 0)
        {
            ShowNoSpellsMessage($"Your Class doesnt use any spells!");
            spellChoicePanel.SetActive(true);
            return;
        }

        // Hide "no spells" message
        if (noSpellsText != null)
        {
            noSpellsText.gameObject.SetActive(false);
        }

        // Update title
        if (titleText != null)
        {
            titleText.text = "Your Spells!";
        }

        // Populate spell items
        PopulateSpells(availableSpells);

        // Show the panel
        spellChoicePanel.SetActive(true);
        
        Debug.Log($"SpellChoiceManager: Opened panel with {availableSpells.Count} spells for {charClass}");
    }

    /// <summary>
    /// Closes the spell choice panel
    /// </summary>
    public void CloseSpellPanel()
    {
        if (spellChoicePanel != null)
        {
            spellChoicePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Populate the spell list with spell items
    /// </summary>
    private void PopulateSpells(List<SpellDefinition> spells)
    {
        if (contentParent == null)
        {
            Debug.LogError("SpellChoiceManager: Content parent is not assigned!");
            return;
        }

        if (spellItemPrefab == null)
        {
            Debug.LogError("SpellChoiceManager: SpellItem prefab is not assigned!");
            return;
        }

        foreach (var spell in spells)
        {
            // Instantiate spell item
            GameObject spellItemObj = Instantiate(spellItemPrefab, contentParent);
            spawnedSpellItems.Add(spellItemObj);

            // Initialize the spell item
            SpellItem spellItem = spellItemObj.GetComponent<SpellItem>();
            if (spellItem != null)
            {
                spellItem.Initialize(spell);
                spellItem.OnSpellSelected += OnSpellItemSelected;
            }
            else
            {
                // If no SpellItem component, try to set text directly
                TextMeshProUGUI textComponent = spellItemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    string diceInfo = spell.diceCount > 0 ? $" ({spell.diceCount}D{spell.diceSize})" : "";
                    textComponent.text = FormatSpellName(spell.spellName.ToString()) + diceInfo;
                }
            }
        }
    }

    /// <summary>
    /// Called when a spell item is selected
    /// </summary>
    private void OnSpellItemSelected(SpellDefinition spell)
    {
        selectedSpell = spell;
        Debug.Log($"SpellChoiceManager: Spell selected - {spell.spellName} ({spell.diceCount}D{spell.diceSize})");
        
        // Trigger the spell cast event
        OnSpellCast?.Invoke(spell);

        // Close the panel after selection
        CloseSpellPanel();
    }

    /// <summary>
    /// Clear all spawned spell items
    /// </summary>
    private void ClearSpellItems()
    {
        foreach (var item in spawnedSpellItems)
        {
            if (item != null)
            {
                // Unsubscribe from events
                SpellItem spellItem = item.GetComponent<SpellItem>();
                if (spellItem != null)
                {
                    spellItem.OnSpellSelected -= OnSpellItemSelected;
                }
                Destroy(item);
            }
        }
        spawnedSpellItems.Clear();
    }

    /// <summary>
    /// Show a "no spells" message
    /// </summary>
    private void ShowNoSpellsMessage(string message)
    {
        ClearSpellItems();
        
        if (noSpellsText != null)
        {
            noSpellsText.text = message;
            noSpellsText.gameObject.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = "Your Spells!";
        }
    }

    /// <summary>
    /// Get the current player's character data
    /// </summary>
    private CharacterData GetPlayerCharacter()
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
                    Debug.Log($"SpellChoiceManager: Using current turn character: {charData.charName} ({charData.charClass})");
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
                    Debug.Log($"SpellChoiceManager: Using selected token's character: {charData.charName} ({charData.charClass})");
                    return charData;
                }
            }
        }

        // Third priority: Try PlayerAssignmentHelper (for networked games)
        if (PlayerAssignmentHelper.Instance != null)
        {
            CharacterData character = PlayerAssignmentHelper.Instance.GetMyCharacter();
            if (character != null)
            {
                Debug.Log($"SpellChoiceManager: Using assigned character: {character.charName} ({character.charClass})");
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the currently selected spell (if any)
    /// </summary>
    public SpellDefinition GetSelectedSpell()
    {
        return selectedSpell;
    }

    /// <summary>
    /// Format spell name from enum (e.g., "MagicMissile" -> "Magic Missile")
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

        ClearSpellItems();

        if (spellAttackButton != null)
        {
            spellAttackButton.onClick.RemoveListener(OpenSpellPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSpellPanel);
        }
    }
}

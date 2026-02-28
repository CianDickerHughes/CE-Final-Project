using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI component for displaying a spell in the spell choice panel.
/// Attach this to the SpellItem prefab.
/// Shows spell name and dice type (e.g., "3D6").
/// </summary>
public class SpellItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI spellNameText;
    [SerializeField] private TextMeshProUGUI diceText;
    [SerializeField] private Button button;

    // The spell definition this item represents
    private SpellDefinition spellDefinition;

    // Event triggered when this spell is selected
    public event Action<SpellDefinition> OnSpellSelected;

    void Awake()
    {
        // Try to find components if not assigned
        if (spellNameText == null)
        {
            spellNameText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // Wire up button click
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// Initialize this spell item with spell data
    /// </summary>
    /// <param name="spell">The spell definition to display</param>
    public void Initialize(SpellDefinition spell)
    {
        spellDefinition = spell;

        // Set spell name - convert enum to readable format
        if (spellNameText != null)
        {
            spellNameText.text = FormatSpellName(spell.spellName.ToString());
        }

        // Set dice info (e.g., "3D6") - only show if spell has dice
        if (diceText != null)
        {
            if (spell.diceCount > 0 && spell.diceSize > 0)
            {
                diceText.text = $"{spell.diceCount}D{spell.diceSize}";
            }
            else
            {
                diceText.text = ""; // No dice for utility spells like Shield
            }
        }
    }

    /// <summary>
    /// Get the spell definition for this item
    /// </summary>
    public SpellDefinition GetSpellDefinition()
    {
        return spellDefinition;
    }

    /// <summary>
    /// Called when the spell item button is clicked
    /// </summary>
    private void OnClick()
    {
        if (spellDefinition != null)
        {
            OnSpellSelected?.Invoke(spellDefinition);
            Debug.Log($"SpellItem: Selected spell {spellDefinition.spellName}");
        }
    }

    /// <summary>
    /// Format spell name from enum (e.g., "MagicMissile" -> "Magic Missile")
    /// </summary>
    private string FormatSpellName(string enumName)
    {
        if (string.IsNullOrEmpty(enumName)) return "";

        // Add space before each capital letter (except the first)
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
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}

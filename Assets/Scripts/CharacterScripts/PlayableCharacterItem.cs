using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for the PlayableCharacterItem prefab used in the WaitingRoom.
/// Displays character info and handles selection state.
/// </summary>
public class PlayableCharacterItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image characterImg;
    [SerializeField] private TextMeshProUGUI charNameText;
    [SerializeField] private TextMeshProUGUI charRaceText;
    [SerializeField] private TextMeshProUGUI charClassText;
    [SerializeField] private Button selectButton;

    [Header("Selection Visual")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 1f, 0.8f, 1f);

    private string filePath;
    private CharacterData characterData;
    private Action<PlayableCharacterItem> onSelected;
    private bool isSelected;

    /// <summary>
    /// The file path to the character JSON file.
    /// </summary>
    public string FilePath => filePath;

    /// <summary>
    /// The loaded character data.
    /// </summary>
    public CharacterData Data => characterData;

    /// <summary>
    /// Whether this item is currently selected.
    /// </summary>
    public bool IsSelected => isSelected;

    /// <summary>
    /// Initialize the item with character data.
    /// </summary>
    /// <param name="jsonFilePath">Path to the character JSON file</param>
    /// <param name="data">Parsed character data</param>
    /// <param name="onSelectCallback">Callback when this item is selected</param>
    public void Setup(string jsonFilePath, CharacterData data, Action<PlayableCharacterItem> onSelectCallback)
    {
        filePath = jsonFilePath;
        characterData = data;
        onSelected = onSelectCallback;

        // Populate UI
        if (charNameText != null)
        {
            charNameText.text = string.IsNullOrEmpty(data.charName) ? "(Unnamed)" : data.charName;
        }

        if (charRaceText != null)
        {
            charRaceText.text = !string.IsNullOrEmpty(data.race) ? data.race : "Unknown";
        }

        if (charClassText != null)
        {
            charClassText.text = !string.IsNullOrEmpty(data.charClass) ? data.charClass : "Unknown";
        }

        // Load token image if available
        if (characterImg != null)
        {
            characterImg.sprite = null;
            characterImg.preserveAspect = true;

            if (!string.IsNullOrEmpty(data.tokenFileName))
            {
                string folder = CharacterIO.GetCharactersFolder();
                string tokenPath = System.IO.Path.Combine(folder, data.tokenFileName);
                if (System.IO.File.Exists(tokenPath))
                {
                    try
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(bytes);
                        characterImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"PlayableCharacterItem: Failed to load token image: {ex.Message}");
                    }
                }
            }
        }

        // Wire up select button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }

        // Reset selection state
        SetSelected(false);
    }

    private void OnSelectClicked()
    {
        onSelected?.Invoke(this);
    }

    /// <summary>
    /// Set the visual selection state of this item.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }

        // Update button text if needed
        if (selectButton != null)
        {
            var buttonText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = selected ? "Selected" : "Select";
            }
        }
    }
}

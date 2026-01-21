using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying a player character in the CampaignManager's CharactersPage.
/// Attach this to the CharacterItem prefab.
/// </summary>
public class PlayerCharacterItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI raceText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private Image characterImage;

    private string filePath;
    private CharacterData characterData;

    /// <summary>
    /// The file path to the character JSON file.
    /// </summary>
    public string FilePath => filePath;

    /// <summary>
    /// The loaded character data.
    /// </summary>
    public CharacterData Data => characterData;

    /// <summary>
    /// Initialize the item with character data.
    /// </summary>
    /// <param name="jsonFilePath">Path to the character JSON file</param>
    /// <param name="data">Parsed character data</param>
    public void Setup(string jsonFilePath, CharacterData data)
    {
        filePath = jsonFilePath;
        characterData = data;

        // Populate UI
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(data.charName) ? "(Unnamed)" : data.charName;
        }

        if (raceText != null)
        {
            raceText.text = !string.IsNullOrEmpty(data.race) ? data.race : "Unknown";
        }

        if (classText != null)
        {
            classText.text = !string.IsNullOrEmpty(data.charClass) ? data.charClass : "Unknown";
        }

        // Load token image if available
        if (characterImage != null && !string.IsNullOrEmpty(data.tokenFileName))
        {
            string folder = System.IO.Path.GetDirectoryName(jsonFilePath);
            string tokenPath = System.IO.Path.Combine(folder, data.tokenFileName);
            
            if (System.IO.File.Exists(tokenPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    characterImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    characterImage.preserveAspect = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"PlayerCharacterItem: Failed to load token image: {ex.Message}");
                }
            }
        }
    }
}

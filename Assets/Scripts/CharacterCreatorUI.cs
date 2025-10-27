using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Simple UI controller for a character creator screen.
// - Exposes UI references to inspector fields (InputFields, Dropdowns, Image, Buttons)
// - Handles loading a token image (editor file picker or simple runtime fallback)
// - Serializes/deserializes CharacterData to/from JSON and saves/loads token images
// - Populates and reads UI fields to build CharacterData
public class CharacterCreatorUI : MonoBehaviour
{
    //Fields for populating the fields in our ui display for the users characters
    //Again this tag just creates a header in the inspector tab we can use
    [Header("UI refs")]
    public TMP_InputField nameInput;
    public TMP_Dropdown raceDropdown;
    public TMP_Dropdown classDropdown;
    public TMP_InputField strengthInput;
    public TMP_InputField dexInput;
    public TMP_InputField conInput;
    public TMP_InputField intInput;
    public TMP_InputField wisInput;
    public TMP_InputField chaInput;
    public Image tokenImage; // UI image to show token
    public Button uploadButton;
    public Button saveButton;
    public Button loadButton;
    public TextMeshProUGUI statusText;

    //Texture for the token to be loaded
    private Texture2D tokenTexture;

    //We then set things up so that on starting we have event listeners on the buttons & clear out everything else - user has a blank canvas
    void Start()
    {
        SetupDropdowns();
        uploadButton.onClick.AddListener(OnUploadClicked);
        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.AddListener(OnLoadClicked);
        ClearStatus();
        tokenTexture = null;
    }

    //This is just a simple method for setting up the drop downs at the moment
    //Could change this later but at the moment we'll have it as a static field
    void SetupDropdowns()
    {
        // Example simple lists; change as needed
        raceDropdown.ClearOptions();
        raceDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Human","Elf","Dwarf","Halfling","Gnome","Half-Orc","Dragonborn"
        });

        classDropdown.ClearOptions();
        classDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Artificer","Barbarian","Bard","Cleric","Druid","Fighter","Monk",
            "Paladin","Ranger","Rogue","Sorcerer","Warlock","Wizard"
        });
    }

    void ClearStatus() => statusText.text = "";

    // Upload image: editor-only file picker, else ask user to paste a file path (simple fallback)
    // When running in the Unity Editor, this opens the native file dialog to pick an image.
    // At runtime (non-editor builds) a very simple prompt coroutine is used instead.
    public void OnUploadClicked()
    {
        #if UNITY_EDITOR
                string path = EditorUtility.OpenFilePanel("Choose token image", "", "png,jpg,jpeg");
                if (string.IsNullOrEmpty(path)) return;
                LoadTextureFromFile(path);
        #else
                // Runtime fallback: prompt user to paste a full file path via a simple popup (or you can implement a runtime file picker).
                StartCoroutine(ShowRuntimePathPrompt());
        #endif
    }

    #if !UNITY_EDITOR
        System.Collections.IEnumerator ShowRuntimePathPrompt()
        {
            statusText.text = "Runtime: please paste full image file path into the Name field and click Upload again.";
            yield return null;
        }
    #endif

    // Load image bytes from disk, convert to Texture2D and set the UI Image sprite.
    // Errors are caught and displayed in the status text.
    void LoadTextureFromFile(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes))
            {
                statusText.text = "Failed to load image.";
                return;
            }
            tokenTexture = tex;
            // assign to UI Image
            Sprite s = SpriteFromTexture2D(tex);
            tokenImage.sprite = s;
            tokenImage.preserveAspect = true;
            statusText.text = "Token loaded.";
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            statusText.text = "Error loading image: " + ex.Message;
        }
    }

    // Helper to create a Sprite from a Texture2D
    Sprite SpriteFromTexture2D(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
    }

    void OnSaveClicked()
    {
        var data = new CharacterData();
        data.charName = nameInput.text;
        data.race = raceDropdown.options[raceDropdown.value].text;
        data.charClass = classDropdown.options[classDropdown.value].text;

        // parse stats with safe fallback to 10
        data.strength = ParseIntOrDefault(strengthInput.text, 10);
        data.dexterity = ParseIntOrDefault(dexInput.text, 10);
        data.constitution = ParseIntOrDefault(conInput.text, 10);
        data.intelligence = ParseIntOrDefault(intInput.text, 10);
        data.wisdom = ParseIntOrDefault(wisInput.text, 10);
        data.charisma = ParseIntOrDefault(chaInput.text, 10);

        // build filenames: name + timestamp
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string baseFileName = $"{(string.IsNullOrEmpty(data.charName) ? "char" : data.charName)}_{timestamp}";

        // save token image if exists
        if (tokenTexture != null)
        {
            string tokenPath = CharacterIO.SaveTokenImage(tokenTexture, baseFileName);
            data.tokenFileName = Path.GetFileName(tokenPath);
        }

        // serialize
        string json = JsonUtility.ToJson(data, true);
        CharacterIO.SaveCharacterJson(json, baseFileName);

        statusText.text = "Saved character.";
    }

    // Load the first saved character JSON found and populate the UI fields.
    // Also attempts to load the associated token image if the JSON references one.
    void OnLoadClicked()
    {
        
        var files = CharacterIO.GetSavedCharacterFilePaths();
        if (files.Length == 0)
        {
            statusText.text = "No saved characters found.";
            return;
        }

        string path = files[0];
        string json = CharacterIO.LoadJsonFromFile(path);
        if (string.IsNullOrEmpty(json))
        {
            statusText.text = "Failed to read file.";
            return;
        }

        CharacterData data = JsonUtility.FromJson<CharacterData>(json);
        PopulateUIFromData(data);
        statusText.text = $"Loaded {data.charName}";
        // load token image if exists
        if (!string.IsNullOrEmpty(data.tokenFileName))
        {
            string folder = CharacterIO.GetCharactersFolder();
            string tokenPath = Path.Combine(folder, data.tokenFileName);
            if (File.Exists(tokenPath))
            {
                LoadTextureFromFile(tokenPath);
            }
        }
    }

    //Populating the ui if we're loading the fields from the data
    void PopulateUIFromData(CharacterData d)
    {
        nameInput.text = d.charName;
        raceDropdown.value = Math.Max(0, raceDropdown.options.FindIndex(o => o.text == d.race));
        classDropdown.value = Math.Max(0, classDropdown.options.FindIndex(o => o.text == d.charClass));
        strengthInput.text = d.strength.ToString();
        dexInput.text = d.dexterity.ToString();
        conInput.text = d.constitution.ToString();
        intInput.text = d.intelligence.ToString();
        wisInput.text = d.wisdom.ToString();
        chaInput.text = d.charisma.ToString();
    }

    int ParseIntOrDefault(string s, int fallback)
    {
        if (int.TryParse(s, out int v)) return v;
        return fallback;
    }
}

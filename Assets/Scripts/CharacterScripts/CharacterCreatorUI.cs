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
// - Populates and reads UI fields to build CharacterData objects
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
    private Texture2D tokenTexture; // loaded token texture

    [Header("Saved Characters Window")]
    public SavedCharactersWindow savedWindow; // assign the panel GameObject with SavedCharactersWindow component

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

    //Method called to setup the dropdown menus with their options
    //Potentially need to change this later to make things more dynamic
    void SetupDropdowns()
    {
        raceDropdown.ClearOptions();
        raceDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Human","Elf","Dwarf","Halfling","Gnome","Half-Orc","Dragonborn", "Tiefling", "Half-Elf"
            , "Other"
        });

        classDropdown.ClearOptions();
        classDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Artificer", "Barbarian", "Bard", "Cleric", "Druid", "Fighter",
            "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard"
        });
    }

    void ClearStatus() => statusText.text = "";

    // Upload image: editor-only file picker, else ask user to paste a file path (simple fallback)
    // When running in the Unity Editor, this opens the native file dialog to pick an image.
    // At runtime (non-editor builds) a very simple prompt coroutine is used instead.
    public void OnUploadClicked()
    {
    #if UNITY_EDITOR
            //We are in the editor - use EditorUtility to open file panel
            string path = EditorUtility.OpenFilePanel("Choose token image", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return;
            LoadTextureFromFile(path);
    #else
            //Runtime fallback: prompt user to paste a full file path via a simple popup (or you can implement a runtime file picker).
            StartCoroutine(ShowRuntimePathPrompt());
    #endif
    }

    #if !UNITY_EDITOR
        System.Collections.IEnumerator ShowRuntimePathPrompt()
        {
            statusText.text = "Runtime: paste full image path into Name field and click Upload again.";
            yield return null;
        }
    #endif

    //This method loads a token from a file path
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
        //parse stats with safe fallback to 10 - the default average stat
        data.strength = ParseIntOrDefault(strengthInput.text, 10);
        data.dexterity = ParseIntOrDefault(dexInput.text, 10);
        data.constitution = ParseIntOrDefault(conInput.text, 10);
        data.intelligence = ParseIntOrDefault(intInput.text, 10);
        data.wisdom = ParseIntOrDefault(wisInput.text, 10);
        data.charisma = ParseIntOrDefault(chaInput.text, 10);

        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string baseFileName = $"{(string.IsNullOrEmpty(data.charName) ? "char" : data.charName)}_{timestamp}";

        // save token image if exists
        if (tokenTexture != null)
        {
            string tokenPath = CharacterIO.SaveTokenImage(tokenTexture, baseFileName);
            data.tokenFileName = Path.GetFileName(tokenPath);
        }

        string json = JsonUtility.ToJson(data, true);
        CharacterIO.SaveCharacterJson(json, baseFileName);

        statusText.text = "Saved character.";
    }

    // NEW: open the saved characters window
    void OnLoadClicked()
    {
        if (savedWindow == null)
        {
            statusText.text = "Saved window not assigned in inspector.";
            return;
        }
        // Open window and provide callback to load selected file
        savedWindow.Open(LoadCharacterFromPath);
    }

    // New method to load from a chosen file path
    void LoadCharacterFromPath(string path)
    {
        if (!File.Exists(path))
        {
            statusText.text = "File not found: " + path;
            return;
        }

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
            else
            {
                tokenTexture = null;
                tokenImage.sprite = null;
            }
        }
        else
        {
            tokenTexture = null;
            tokenImage.sprite = null;
        }
    }

    //This is another new method to populate the UI fields from a CharacterData object
    //Literally all it does is fill in the various fields with the data from the object/loaded character
    void PopulateUIFromData(CharacterData d)
    {
        nameInput.text = d.charName;
        raceDropdown.value = Math.Max(0, raceDropdown.options.FindIndex(o => o.text == d.race));
        classDropdown.value = Math.Max(0, classDropdown.options.FindIndex(o => o.text == d.@charClass));
        strengthInput.text = d.strength.ToString();
        dexInput.text = d.dexterity.ToString();
        conInput.text = d.constitution.ToString();
        intInput.text = d.intelligence.ToString();
        wisInput.text = d.wisdom.ToString();
        chaInput.text = d.charisma.ToString();
    }

    //This is a small utility method to parse an int from a string with a fallback value
    int ParseIntOrDefault(string s, int fallback)
    {
        if (int.TryParse(s, out int v)) return v;
        return fallback;
    }
}

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterCreatorUI : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_InputField nameInput;
    public TextMeshProUGUI nameText;
    public TMP_Dropdown raceDropdown;
    public TextMeshProUGUI raceText;
    public TMP_Dropdown classDropdown;
    public TextMeshProUGUI classText;
    public TMP_Dropdown weaponDropdown;
    public TMP_InputField strengthInput;
    public TextMeshProUGUI strengthText;
    public TMP_InputField dexInput;
    public TextMeshProUGUI dexText;
    public TMP_InputField conInput;
    public TextMeshProUGUI conText;
    public TMP_InputField intInput;
    public TextMeshProUGUI intText;
    public TMP_InputField wisInput;
    public TextMeshProUGUI wisText;
    public TMP_InputField chaInput;
    public TextMeshProUGUI chaText;
    public TMP_InputField levelInput;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI ACText;
    public TextMeshProUGUI HPText;
    private int AC;
    private int HP;
    private string[] weapons;
    public Image tokenImage;
    public Button uploadButton;
    public Button saveButton;
    public TextMeshProUGUI statusText;
    private int conMod;

    // When editing an existing character, this will hold the JSON file path to overwrite.
    private string editFilePath = null;
    private Texture2D tokenTexture;

    void Start()
    {
        SetupDropdowns();
        if (uploadButton != null)
            uploadButton.onClick.AddListener(OnUploadClicked);
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
        ClearStatus();
        tokenTexture = null;

        // If the selection context has a path, this is Edit mode: load it
        if (!string.IsNullOrEmpty(CharacterSelectionContext.SelectedCharacterFilePath))
        {
            editFilePath = CharacterSelectionContext.SelectedCharacterFilePath;
            LoadCharacterFromPath(editFilePath);
        }
        else
        {
            // New character: clear UI
            ClearUI();
        }
    }

    //Method called to setup the dropdown menus with their options
    //Potentially need to change this later to make things more dynamic
    void SetupDropdowns()
    {
        if (raceDropdown != null)
        {
            raceDropdown.ClearOptions();
            raceDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Human","Elf","Dwarf","Halfling","Gnome", "Githyanki","Half-Orc","Dragonborn", "Half-Elf","Tiefling"
                , "Aasimar","Goliath","Tabaxi","Orc"
            });
        }

        if (classDropdown != null)
        {
            classDropdown.ClearOptions();
            classDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Artificer", "Barbarian", "Bard", "Cleric", "Druid", "Fighter",
                "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Wizard"
            });
        }

        if (weaponDropdown != null)
        {
            weaponDropdown.ClearOptions();
            weaponDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Broadsword", "Greatsword", "Axe", "Bow", "Crossbow", "Dagger", "Staff"
            });
        }
    }

    void ClearStatus() 
    { 
        if (statusText != null) 
            statusText.text = ""; 
    }


    void ClearUI()
    {
        if (nameInput != null) nameInput.text = "";
        if (raceDropdown != null) raceDropdown.value = 0;
        if (classDropdown != null) classDropdown.value = 0;
        if (strengthInput != null) strengthInput.text = "10";
        if (dexInput != null) dexInput.text = "10";
        if (conInput != null) conInput.text = "10";
        if (intInput != null) intInput.text = "10";
        if (wisInput != null) wisInput.text = "10";
        if (chaInput != null) chaInput.text = "10";
        if (levelInput != null) levelInput.text = "1";
        if (levelText != null) levelText.text = "Level: 1";
        if (tokenImage != null) tokenImage.sprite = null;
        if (nameText != null) nameText.text = "";
        if (raceText != null) raceText.text = "";
        if (classText != null) classText.text = "";
        if (strengthText != null) strengthText.text = "10";
        if (dexText != null) dexText.text = "10";
        if (conText != null) conText.text = "10";
        if (intText != null) intText.text = "10";
        if (wisText != null) wisText.text = "10";
        if (chaText != null) chaText.text = "10";
    }

    // Upload image: editor-only file picker, else ask user to paste a file path (simple fallback)
    // When running in the Unity Editor, this opens the native file dialog to pick an image.
    // At runtime (non-editor builds) a simple UI dialog is shown instead.
    public void OnUploadClicked()
    {
        FilePickerHelper.PickImageFile(LoadTextureFromFile);
    }

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
            tokenImage.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(.5f,.5f));
            tokenImage.preserveAspect = true;
            statusText.text = "Token loaded.";
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            statusText.text = "Error loading image: " + ex.Message;
        }
    }

    void OnSaveClicked()
    {
        var data = new CharacterData();
        data.charName = nameInput.text;
        data.race = raceDropdown.options[raceDropdown.value].text;
        data.charClass = classDropdown.options[classDropdown.value].text;
        data.strength = ParseIntOrDefault(strengthInput.text, 10);
        data.dexterity = ParseIntOrDefault(dexInput.text, 10);
        data.constitution = ParseIntOrDefault(conInput.text, 10);
        data.intelligence = ParseIntOrDefault(intInput.text, 10);
        data.wisdom = ParseIntOrDefault(wisInput.text, 10);
        data.charisma = ParseIntOrDefault(chaInput.text, 10);
        data.level = ParseIntOrDefault(levelInput.text, 1);
        data.speed = 30; // Default speed, could be modified later 
        data.weapon = weaponDropdown.options[weaponDropdown.value].text;

        //Race Specific Bonuses
        switch(data.race)
        {
            case "Dragonborn":
                data.strength += 2;
                data.charisma += 1;
                break;
            case "Dwarf":
                data.constitution += 2;
                break;
            case "Elf":
                data.dexterity += 2;
                break;
            case "Gnome":
                data.intelligence += 2;
                break;
            case "Half-Elf":
                data.charisma += 2;
                data.strength += 1;
                data.dexterity += 1;
                break;
            case "Half-Orc":
                data.strength += 2;
                data.constitution += 1;
                break;
            case "Halfling":
                data.dexterity += 2;
                break;
            case "Human":
                data.strength += 1;
                data.dexterity += 1;
                data.constitution += 1;
                data.intelligence += 1;
                data.wisdom += 1;
                data.charisma += 1;
                break;
            case "Tiefling":
                data.charisma += 2;
                break;
            case "Aasimar":
                data.charisma += 2;
                data.strength += 1;
                break;
            case "Goliath":
                data.strength += 2;
                data.constitution += 1;
                break;
            case "Tabaxi":
                data.dexterity += 2;
                data.charisma += 1;
                break;
            case "Githyanki":
                data.strength += 2;
                data.intelligence += 1;
                break;
        }

        //Class Specific Bonuses & Spells - for simplicity I'll restrict things to only a handful of spells/abilities for now - I could greatly expand upon this
        //Also setting up things like spell slots here - number per level
        switch(data.charClass)
        {
            case "Artificer":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Infuse Item" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;

                break;
            case "Barbarian":
                data.spellSlots = new Dictionary<string, int>();
                data.abilities = new List<string> { "Rage" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }

                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 12 * ParseIntOrDefault(levelInput.text, 1) + conMod;

                break;
            case "Bard":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Bardic Inspiration" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }

                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Cleric":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Channel Divinity" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }

                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Druid":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Wild Shape" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }

                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Fighter":
                data.spellSlots = new Dictionary<string, int>(); // No spell slots for Fighter
                data.abilities = new List<string> { "Action Surge" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }

                //Calculating the Health Pool based on Constitution modifier/score
                conMod = (data.constitution - 10) / 2;
                data.HP = 10 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Monk":
                data.spellSlots = new Dictionary<string, int>(); // No spell slots for Monk
                data.abilities = new List<string> { "Martial Arts" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Paladin":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Lay on Hands" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 10 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Ranger":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Favored Enemy" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 10 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Rogue":
                data.spellSlots = new Dictionary<string, int>(); // No spell slots for Rogue
                data.abilities = new List<string> {"Sneak Attack"};
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Sorcerer":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Spellcasting" };
                if(data.level <=3 && data.level >= 1)
                {                    
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 6 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Warlock":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Otherworldly Patron", "Pact Magic" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 8 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
            case "Wizard":
                data.spellSlots = new Dictionary<string, int>
                {
                    { "1st", 2 },
                    { "2nd", 0 },
                    { "3rd", 0 }
                };
                data.abilities = new List<string> { "Arcane Recovery" };
                if(data.level <=3 && data.level >= 1)
                {
                    data.proficiencyBonus = 2;
                }
                else if(data.level > 3 && data.level <= 8)
                {
                    data.proficiencyBonus = 3;
                }
                else
                {
                    data.proficiencyBonus = 4;
                }
                conMod = (data.constitution - 10) / 2;
                data.HP = 6 * ParseIntOrDefault(levelInput.text, 1) + conMod;
                break;
        }

        //Calculating Armor Class based on Dexterity modifier/score
        //Possibly add more detail to this later - what class is this character, do they have a shield?
        int dexMod = (data.dexterity - 10) / 2;
        data.AC = 10 + dexMod;

        // If editFilePath is null -> create new file; else overwrite the existing file
        if (string.IsNullOrEmpty(editFilePath))
        {
            // New: create with timestamp name (same as earlier)
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string baseFileName = $"{(string.IsNullOrEmpty(data.charName) ? "char" : data.charName)}_{timestamp}";

            if (tokenTexture != null)
            {
                string tokenPath = CharacterIO.SaveTokenImage(tokenTexture, baseFileName);
                data.tokenFileName = Path.GetFileName(tokenPath);
            }

            string json = JsonUtility.ToJson(data, true);
            CharacterIO.SaveCharacterJson(json, baseFileName);
            statusText.text = "Saved new character.";
        }
        else
        {
            // Edit: overwrite existing file. Keep token filename if present (or replace).
            // Read existing to preserve token filename if user didn't change the token
            string existingJson = CharacterIO.LoadJsonFromFile(editFilePath);
            CharacterData existing = null;
            try { existing = JsonUtility.FromJson<CharacterData>(existingJson); } catch { existing = null; }

            // If tokenTexture is set, write a new token file using the existing filename or a derived name
            if (tokenTexture != null)
            {
                // use existing tokenFileName if present; else create one based on existing json filename
                string baseName = Path.GetFileNameWithoutExtension(editFilePath);
                string tokenPath = CharacterIO.SaveTokenImage(tokenTexture, baseName);
                data.tokenFileName = Path.GetFileName(tokenPath);
            }
            else
            {
                // preserve old token filename if present
                if (existing != null)
                    data.tokenFileName = existing.tokenFileName;
            }

            // Preserve id and createdAt from existing if possible
            if (existing != null)
            {
                data.id = existing.id;
                data.createdAt = existing.createdAt;
            }
            else
            {
                // fallback to new Guid/time
                data.id = Guid.NewGuid().ToString();
                data.createdAt = DateTime.UtcNow.ToString("o");
            }

            // Serialize and overwrite the same file
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(editFilePath, json);
            statusText.text = "Saved changes to character.";
        }

        SceneManager.LoadSceneAsync("Characters");
    }

    //Loads a character JSON from a path and populates UI
    void LoadCharacterFromPath(string path)
    {
        if (!File.Exists(path))
        {
            statusText.text = "File not found.";
            return;
        }

        string json = CharacterIO.LoadJsonFromFile(path);
        if (string.IsNullOrEmpty(json))
        {
            statusText.text = "Failed to load file.";
            return;
        }

        CharacterData data = JsonUtility.FromJson<CharacterData>(json);
        PopulateUIFromData(data);
    }

    //This is another new method to populate the UI fields from a CharacterData object
    //Literally all it does is fill in the various fields with the data from the object/loaded character
    void PopulateUIFromData(CharacterData d)
    {
        nameInput.text = d.charName;
        raceDropdown.value = Math.Max(0, raceDropdown.options.FindIndex(o => o.text == d.race));
        raceText.text = "Race: " + d.race.ToString();
        classDropdown.value = Math.Max(0, classDropdown.options.FindIndex(o => o.text == d.charClass));
        classText.text = "Class: " + d.charClass.ToString();
        strengthInput.text = d.strength.ToString();
        strengthText.text = d.strength.ToString();
        dexInput.text = d.dexterity.ToString();
        dexText.text = d.dexterity.ToString();
        conInput.text = d.constitution.ToString();
        conText.text = d.constitution.ToString();
        intInput.text = d.intelligence.ToString();
        intText.text = d.intelligence.ToString();
        wisInput.text = d.wisdom.ToString();
        wisText.text = d.wisdom.ToString();
        chaInput.text = d.charisma.ToString();
        chaText.text = d.charisma.ToString();
        levelInput.text = d.level.ToString();
        levelText.text = "Level: " + d.level.ToString();
        ACText.text = "AC: " + d.AC.ToString();
        HPText.text = "HP: " + d.HP.ToString();

        // Load token if exists
        if (!string.IsNullOrEmpty(d.tokenFileName))
        {
            string folder = CharacterIO.GetCharactersFolder();
            string tokenPath = Path.Combine(folder, d.tokenFileName);
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

    //This is a small utility method to parse an int from a string with a fallback valuey
    int ParseIntOrDefault(string s, int fallback)
    {
        if (int.TryParse(s, out int v)) return v;
        return fallback;
    }
}

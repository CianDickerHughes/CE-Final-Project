using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

//Script is meant for the simple prefab item to choose a character to fight the DM with
public class DMFightCharItem: MonoBehaviour
{
    //References to the UI elements in the prefab
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterClass;
    public Image charImg;
    public Button selectForFightButton;
    private string filePath;

    //Reference to the character data this item represents
    public CharacterData characterData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //Setup method to set up the UI elements based on the character data
    public void Setup(CharacterData data)
    {
        characterData = data;
        characterName.text = data.charName;
        characterClass.text = data.charClass;
        //Here you would set the character image based on the data - for now we can just use a placeholder
        charImg.sprite = Resources.Load<Sprite>("PlaceholderCharacterImage");
    }

    //Setup called after instantiation
    public void Setup(string jsonFilePath, CharacterData data, Action<string> onSelect)
    {
        filePath = jsonFilePath;
        characterData = data;

        if (characterName != null)
        {
            characterName.text = string.IsNullOrEmpty(data.charName) ? "(Unnamed)" : data.charName;
        }

        if (characterClass != null)
        {
            characterClass.text = !string.IsNullOrEmpty(data.charClass) ? data.charClass : "";
        }

        // load thumbnail image if available (async is not required here for local files)
        if (charImg != null)
        {
            charImg.sprite = null;
            charImg.preserveAspect = true;

            if (!string.IsNullOrEmpty(data.tokenFileName))
            {
                string folder = CharacterIO.GetCharactersFolder();
                string tokenPath = System.IO.Path.Combine(folder, data.tokenFileName);
                if (System.IO.File.Exists(tokenPath))
                {
                    try
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                        Texture2D tex = new Texture2D(2,2);
                        tex.LoadImage(bytes);
                        charImg.sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(.5f,.5f));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Failed to load token thumbnail: " + ex.Message);
                    }
                }
            }
        }

        //Wiring up the button to call the onSelect action with the file path when clicked
        //Selecting this character for the fight will load the character data from the file path and set it as the current character for the DM fight
        //Then move them to the appropriate scene to start the fight
        if (selectForFightButton != null)
        {
            //selectForFightButton.onClick.RemoveAllListeners();
            //selectForFightButton.onClick.AddListener(() => onSelect?.Invoke(filePath));
        }
    }
}

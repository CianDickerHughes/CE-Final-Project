using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SavedCharacterItem : MonoBehaviour
{
    //Variables for UI references - the stuff we will actually display from the loaded characters for the user
    public Image thumbImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI charClass;
    public TextMeshProUGUI charRace;
    public Button loadButton;
    private string filePath;

    // Setup called after instantiation
    public void Setup(string jsonFilePath, CharacterData data, Action<string> onSelect)
    {
        filePath = jsonFilePath;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(data.charName) ? "(Unnamed)" : data.charName;
        }

        if (timestampText != null)
        {
            timestampText.text = !string.IsNullOrEmpty(data.createdAt) ? data.createdAt : "";
        }

        if (charClass != null)
        {
            charClass.text = !string.IsNullOrEmpty(data.charClass) ? data.charClass : "";
        }

        if (charRace != null)
        {
            charRace.text = !string.IsNullOrEmpty(data.race) ? data.race : "";
        }

        // load thumbnail image if available (async is not required here for local files)
        if (thumbImage != null)
        {
            thumbImage.sprite = null;
            thumbImage.preserveAspect = true;

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
                        thumbImage.sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(.5f,.5f));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Failed to load token thumbnail: " + ex.Message);
                    }
                }
            }
        }

        // wire up the load button
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() => onSelect?.Invoke(filePath));
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SavedCharacterItem : MonoBehaviour
{
    //Variables for UI references - the stuff we will actually display from the loaded characters for the user
    public Image thumbImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI charClass;
    public TextMeshProUGUI charRace;
    public Button loadButton;
    public Button spawnButton;
    public Button deleteButton;
    private string filePath;
    private CharacterData characterData;
    private Action onDelete;

    //Setup called after instantiation
    public void Setup(string jsonFilePath, CharacterData data, Action<string> onSelect, Action onDelete = null)
    {
        filePath = jsonFilePath;
        characterData = data;
        this.onDelete = onDelete;

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

        //Wiring up the spawn button
        if(spawnButton != null){
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(() => SelectForSpawning(CharacterType.Player));
        }

        // Wire up the delete button
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }

    public void SelectForSpawning(CharacterType type){
        if (TokenManager.Instance != null && characterData != null)
        {
            TokenManager.Instance.SetSelectedForSpawn(characterData, CharacterType.NPC);
            Debug.Log($"Selected {characterData.charName} for spawning.");
        } 
    }

    private void OnDeleteClicked()
    {
        #if UNITY_EDITOR
        // Confirm deletion with editor dialog
        if (!EditorUtility.DisplayDialog(
            "Delete Character",
            $"Are you sure you want to delete '{characterData.charName}'? This cannot be undone.",
            "Delete",
            "Cancel"))
        {
            return;
        }
        #endif

        // Delete the character files
        if (CharacterIO.DeleteCharacter(filePath))
        {
            Debug.Log($"Character {characterData.charName} deleted successfully.");
            onDelete?.Invoke();
            // Destroy this UI item
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Failed to delete character.");
        }
    }
}

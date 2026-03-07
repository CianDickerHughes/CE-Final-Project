using UnityEngine;

//Simple static holder to pass the selected file path between scenes.
//Minimal and easy to understand; not persistent across runs.
public static class CharacterSelectionContext
{
    //The JSON file path of the selected character. Set before loading EditCharacter scene.
    public static string SelectedCharacterFilePath = null;

    //The actual CharacterData object - used for DMFight and other scenes that need the full data
    public static CharacterData SelectedCharacterData = null;

    //This method clears the selection - helps us when cleaning up
    public static void Clear()
    {
        SelectedCharacterFilePath = null;
        SelectedCharacterData = null;
    }

    //Set the selected character data directly (for DMFight flow)
    public static void SetSelectedCharacter(CharacterData data)
    {
        SelectedCharacterData = data;
    }

    //Get the selected character data - loads from file if only path is set
    public static CharacterData GetSelectedCharacter()
    {
        //If we have direct data, return it
        if (SelectedCharacterData != null)
        {
            return SelectedCharacterData;
        }

        //Otherwise try to load from file path
        if (!string.IsNullOrEmpty(SelectedCharacterFilePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(SelectedCharacterFilePath);
                SelectedCharacterData = JsonUtility.FromJson<CharacterData>(json);
                return SelectedCharacterData;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Failed to load character from file: " + ex.Message);
            }
        }

        return null;
    }
}

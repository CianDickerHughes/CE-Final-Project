using UnityEngine;

//Simple static holder to pass the selected file path between scenes.
//Minimal and easy to understand; not persistent across runs.
public static class CharacterSelectionContext
{
    //The JSON file path of the selected character. Set before loading EditCharacter scene.
    public static string SelectedCharacterFilePath = null;

    //This method clears the selection - helps us when cleaning up
    public static void Clear() => SelectedCharacterFilePath = null;
}

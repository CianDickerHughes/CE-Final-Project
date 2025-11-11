using System.IO;
using UnityEngine;
using System;

//This file actually handles the complex stuff of saving the files to the characters folder for now
//Need to change this later to saving them to the users device
//It also separates character data (JSON) from visual assets (PNG images) and provides platform-aware
public class CharacterIO : MonoBehaviour
{
    //Returns the folder path to save characters.
    //We first check to see where we are running (Editor vs Build)
    //If we're in Editor we save to Assets/characters so the files are visible in the project.
    //If we're in a build we save to Application.persistentDataPath/characters
    public static string GetCharactersFolder()
    {
        #if UNITY_EDITOR
                // Application.dataPath = "ProjectRoot/Assets" - allows direct file inspection during development
                string folder = Path.Combine(Application.dataPath, "characters");
        #else
                // persistentDataPath ensures data survives app updates and is writable on all platforms
                // Location varies by platform (e.g., AppData on Windows, Documents on mobile)
                string folder = Path.Combine(Application.persistentDataPath, "characters");
        #endif
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                return folder;
    }

    // Save JSON text to file
    public static string SaveCharacterJson(string json, string baseFileName)
    {
        string folder = GetCharactersFolder();
        //File name - sanitize to avoid invalid characters - essentially replace invalid chars with underscores
        string filename = $"{SanitizeFileName(baseFileName)}.json";
        //We then just combine the 2 into a full path
        string full = Path.Combine(folder, filename);
        //Write the file
        File.WriteAllText(full, json);
        //Then we log it and return the full path
        Debug.Log($"Saved character JSON to: {full}");
        return full;
    }

    //Save texture2D as PNG
    public static string SaveTokenImage(Texture2D tex, string baseFileName)
    {
        if (tex == null){
            Debug.LogWarning("No texture provided to save as token image.");
            return null;
        }

        //This will help us convert the texture to PNG format - EncodeToPNG()
        byte[] bytes = tex.EncodeToPNG();
        string folder = GetCharactersFolder();
        //We then get the filename of the token image by appending _token to the base file name
        string filename = $"{SanitizeFileName(baseFileName)}_token.png";
        string full = Path.Combine(folder, filename);
        File.WriteAllBytes(full, bytes);
        Debug.Log($"Saved token image to: {full}");
        return full;
    }

    //Load JSON text
    //Uses *.json pattern to filter only character files, ignoring images or other data so we only sift through what we want
    public static string[] GetSavedCharacterFilePaths()
    {
        string folder = GetCharactersFolder();
        return Directory.GetFiles(folder, "*.json");
    }

    //This method will help us load the JSON text from a file path
    public static string LoadJsonFromFile(string path)
    {
        //If it doesn't exist, log a warning and return null
        if (!File.Exists(path))
        {
            Debug.LogWarning($"File not found at path: {path}");
            return null;
        }

        //If it does then we just read all text and return it
        return File.ReadAllText(path);
    }

    //Sanitize file names to avoid invalid characters
    public static string SanitizeFileName(string name)
    {
        //For each invalid character, replace it with an underscore
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "_");
        }
        //After the reworking is done we just return the new name
        return name;
    }
}

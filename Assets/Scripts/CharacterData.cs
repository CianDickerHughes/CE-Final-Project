using System;
using UnityEngine;

//All this class does is handle the various stuff to do with the actual things relating to a character - their actual fields/data

//THIS CLASS IS SUBJECT TO CHANGE
//When we move characters from being saved in game to the users individual device we may need to change this file around
[Serializable]
public class CharacterData : MonoBehaviour
{
    //Unique ID for each of the characters
    public string id;
    //RName, race & class
    public string charName;
    public string race;
    public string charClass;
    //Individual stats for the character
    public int strength;
    public int dexterity;
    public int constitution;
    public int intelligence;
    public int wisdom;
    public int charisma;
    //String for handling the image relating to this persons character
    public string tokenFileName;
    //Timestamp for creating things
    public string createdAt;

    public CharacterData()
    {
        id = Guid.NewGuid().ToString();
        createdAt = DateTime.UtcNow.ToString("o");
    }
}

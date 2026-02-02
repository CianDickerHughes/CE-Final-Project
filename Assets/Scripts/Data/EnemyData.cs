using UnityEngine;
using System;

[Serializable]
public class EnemyData
{
    //Unique ID for each enemy
    public string id;
    //Name, type, and challenge rating
    public string name;
    public string type;
    public string challengeRating;
    public string size;
    public string alignment;
    //Ability scores
    public AbilityScores abilityScores;
    //String for handling the image relating to this enemy
    public string tokenFileName;
    //Immunities
    public string[] immunities;
    //Game-related stats
    public int hp;
    public int armor_class;
    public int speed;

    public EnemyData()
    {
        id = Guid.NewGuid().ToString();
    }
}

//Nested class for the Ability Scores
//Maybe share this/change this later to be used by both characters and enemies
[Serializable]
public class AbilityScores
{
    public int STR;
    public int DEX;
    public int CON;
    public int INT;
    public int WIS;
    public int CHA;
}

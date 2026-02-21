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
    public EnemyType enemyType;
    public string challengeRating;
    public string size;
    public string alignment;
    //String for handling the image relating to this enemy
    public string tokenFileName;
    //Immunities
    public string[] immunities;
    //Game-related stats
    public int HP;
    public int AC;
    public int speed;
    //Ability scores
    public int strength;
    public int dexterity;
    public int constitution;
    public int intelligence;
    public int wisdom;
    public int charisma;

    public EnemyData()
    {
        id = Guid.NewGuid().ToString();
    }
}

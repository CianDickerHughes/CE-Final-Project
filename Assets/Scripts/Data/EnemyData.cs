using UnityEngine;
using System;

[Serializable]
public class EnemyData
{
    //Unique ID for each enemy
    public string id;
    //Name, type, and challenge rating
    public string enemyName;
    public string enemyType;
    public string challengeRating;
    //String for handling the image relating to this enemy
    public string iconFileName;

    public EnemyData()
    {
        id = Guid.NewGuid().ToString();
    }
}

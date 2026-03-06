using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AbilityData
{
    public string abilityName;
    public string description;
    public AbilityType abilityType;
    //Dice needed to roll for effects
    public string diceRoll;
    public int flatBonus; 
    public bool requiresTarget;
    public int usesPerRest;
    public string targetType;
}
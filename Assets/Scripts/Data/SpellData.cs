using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SpellDefinition
{
    public SpellName spellName;
    public SpellType spellType;
    public int spellLevel;
    public List<string> allowedClasses;
    //Damage and healing dice
    public int diceCount;
    public int diceSize;

    //Potentiall later we can add fields for:
    //public int range;
    //public string description;
}

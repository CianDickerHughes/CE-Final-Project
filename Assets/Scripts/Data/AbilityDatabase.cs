using UnityEngine;
using System.Collections.Generic;

public static class AbilityDatabase
{
    private static Dictionary<string, AbilityData> abilities = new Dictionary<string, AbilityData>
    {
        //Artificer
        { "Infuse Item", new AbilityData {
            abilityName = "Infuse Item",
            description = "Infuse an item with magical properties",
            abilityType = AbilityType.Utility,
            requiresTarget = false,
            usesPerRest = 2,
            targetType = "Self"
        }},
        
        //Barbarian
        { "Rage", new AbilityData {
            abilityName = "Rage",
            description = "Enter a rage, gaining bonus damage and resistance",
            abilityType = AbilityType.Buff,
            flatBonus = 2,
            requiresTarget = false,
            usesPerRest = 2,
            targetType = "Self"
        }},
        
        //Bard
        { "Bardic Inspiration", new AbilityData {
            abilityName = "Bardic Inspiration",
            description = "Inspire an ally with a bonus die",
            abilityType = AbilityType.Buff,
            diceRoll = "1d6",
            requiresTarget = true,
            usesPerRest = 3,
            targetType = "Ally"
        }},
        
        //Cleric
        { "Channel Divinity", new AbilityData {
            abilityName = "Channel Divinity",
            description = "Channel divine energy for various effects",
            abilityType = AbilityType.Healing,
            diceRoll = "2d8",
            requiresTarget = true,
            usesPerRest = 1,
            targetType = "Any"
        }},
        
        //Druid
        { "Wild Shape", new AbilityData {
            abilityName = "Wild Shape",
            description = "Transform into a beast",
            abilityType = AbilityType.Utility,
            requiresTarget = false,
            usesPerRest = 2,
            targetType = "Self"
        }},
        
        //Fighter
        { "Action Surge", new AbilityData {
            abilityName = "Action Surge",
            description = "Take an additional action this turn",
            abilityType = AbilityType.Buff,
            requiresTarget = false,
            usesPerRest = 1,
            targetType = "Self"
        }},
        
        //Monk
        { "Martial Arts", new AbilityData {
            abilityName = "Martial Arts",
            description = "Make an unarmed strike as a bonus action",
            abilityType = AbilityType.Damage,
            diceRoll = "1d4",
            requiresTarget = true,
            usesPerRest = -1,
            targetType = "Enemy"
        }},
        
        //Paladin
        { "Lay on Hands", new AbilityData {
            abilityName = "Lay on Hands",
            description = "Heal a creature with your touch",
            abilityType = AbilityType.Healing,
            flatBonus = 5,
            requiresTarget = true,
            usesPerRest = 1,
            targetType = "Ally"
        }},
        
        //Ranger
        { "Favored Enemy", new AbilityData {
            abilityName = "Favored Enemy",
            description = "Deal extra damage to your favored enemy type",
            abilityType = AbilityType.Damage,
            diceRoll = "1d6",
            requiresTarget = true,
            usesPerRest = -1,
            targetType = "Enemy"
        }},
        
        //Rogue
        { "Sneak Attack", new AbilityData {
            abilityName = "Sneak Attack",
            description = "Deal extra damage when you have advantage",
            abilityType = AbilityType.Damage,
            diceRoll = "2d6",
            requiresTarget = true,
            usesPerRest = -1,
            targetType = "Enemy"
        }},
        
        //Sorcerer
        { "Spellcasting", new AbilityData {
            abilityName = "Spellcasting",
            description = "Cast spells using sorcery points",
            abilityType = AbilityType.Utility,
            requiresTarget = false,
            usesPerRest = -1,
            targetType = "Self"
        }},
        
        //Warlock
        { "Pact Magic", new AbilityData {
            abilityName = "Pact Magic",
            description = "Cast spells using pact magic slots",
            abilityType = AbilityType.Utility,
            requiresTarget = false,
            usesPerRest = -1,
            targetType = "Self"
        }},
        
        //Wizard
        { "Arcane Recovery", new AbilityData {
            abilityName = "Arcane Recovery",
            description = "Recover spell slots",
            abilityType = AbilityType.Utility,
            requiresTarget = false,
            usesPerRest = 1,
            targetType = "Self"
        }}
    };

    public static AbilityData GetAbility(string abilityName)
    {
        if (abilities.TryGetValue(abilityName, out AbilityData ability))
        {
            return ability;
        }
        return null;
    }

    public static List<AbilityData> GetAbilitiesForClass(string charClass)
    {
        List<AbilityData> classAbilities = new List<AbilityData>();
        
        switch (charClass)
        {
            case "Artificer":
                classAbilities.Add(GetAbility("Infuse Item"));
                break;
            case "Barbarian":
                classAbilities.Add(GetAbility("Rage"));
                break;
            case "Bard":
                classAbilities.Add(GetAbility("Bardic Inspiration"));
                break;
            case "Cleric":
                classAbilities.Add(GetAbility("Channel Divinity"));
                break;
            case "Druid":
                classAbilities.Add(GetAbility("Wild Shape"));
                break;
            case "Fighter":
                classAbilities.Add(GetAbility("Action Surge"));
                break;
            case "Monk":
                classAbilities.Add(GetAbility("Martial Arts"));
                break;
            case "Paladin":
                classAbilities.Add(GetAbility("Lay on Hands"));
                break;
            case "Ranger":
                classAbilities.Add(GetAbility("Favored Enemy"));
                break;
            case "Rogue":
                classAbilities.Add(GetAbility("Sneak Attack"));
                break;
            case "Sorcerer":
                classAbilities.Add(GetAbility("Spellcasting"));
                break;
            case "Warlock":
                classAbilities.Add(GetAbility("Pact Magic"));
                break;
            case "Wizard":
                classAbilities.Add(GetAbility("Arcane Recovery"));
                break;
        }
        
        return classAbilities;
    }
}
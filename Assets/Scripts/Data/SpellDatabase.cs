using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

//Centralized database for all spell definitions.
//Provides lookup methods for spells by name, class, and level.
public static class SpellDatabase
{
    private static Dictionary<SpellName, SpellDefinition> spells;

    //Constructor to initialize the spell database
    static SpellDatabase()
    {
        InitializeSpells();
    }

    //Method for initializing spells into the list
    private static void InitializeSpells()
    {
        spells = new Dictionary<SpellName, SpellDefinition>
        {
            //=== CANTRIPS (Level 0) ===
            {
                SpellName.EldritchBlast, new SpellDefinition
                {
                    spellName = SpellName.EldritchBlast,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Warlock" },
                    diceCount = 1,
                    diceSize = 10
                }
            },
            {
                SpellName.FireBolt, new SpellDefinition
                {
                    spellName = SpellName.FireBolt,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard", "Artificer" },
                    diceCount = 1,
                    diceSize = 10
                }
            },
            {
                SpellName.Guidance, new SpellDefinition
                {
                    spellName = SpellName.Guidance,
                    spellType = SpellType.Utility,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Cleric", "Druid", "Artificer" },
                    diceCount = 1,
                    diceSize = 4
                }
            },
            {
                SpellName.RayOfFrost, new SpellDefinition
                {
                    spellName = SpellName.RayOfFrost,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard", "Artificer" },
                    diceCount = 1,
                    diceSize = 8
                }
            },
            {
                SpellName.SacredFlame, new SpellDefinition
                {
                    spellName = SpellName.SacredFlame,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 1,
                    diceSize = 8
                }
            },
            {
                SpellName.SpareTheDying, new SpellDefinition
                {
                    spellName = SpellName.SpareTheDying,
                    spellType = SpellType.Healing,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Cleric", "Artificer" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.ThornWhip, new SpellDefinition
                {
                    spellName = SpellName.ThornWhip,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Druid", "Artificer" },
                    diceCount = 1,
                    diceSize = 6
                }
            },
            {
                SpellName.ViciousMockery, new SpellDefinition
                {
                    spellName = SpellName.ViciousMockery,
                    spellType = SpellType.Damage,
                    spellLevel = 0,
                    allowedClasses = new List<string> { "Bard" },
                    diceCount = 1,
                    diceSize = 4
                }
            },

            //=== 1ST LEVEL SPELLS ===
            {
                SpellName.ArmorOfAgathys, new SpellDefinition
                {
                    spellName = SpellName.ArmorOfAgathys,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Warlock" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.Bless, new SpellDefinition
                {
                    spellName = SpellName.Bless,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Cleric", "Paladin" },
                    diceCount = 1,
                    diceSize = 4
                }
            },
            {
                SpellName.BurningHands, new SpellDefinition
                {
                    spellName = SpellName.BurningHands,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 3,
                    diceSize = 6
                }
            },
            {
                SpellName.CureWounds, new SpellDefinition
                {
                    spellName = SpellName.CureWounds,
                    spellType = SpellType.Healing,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Bard", "Cleric", "Druid", "Paladin", "Ranger", "Artificer" },
                    diceCount = 1,
                    diceSize = 8
                }
            },
            {
                SpellName.DissonantWhispers, new SpellDefinition
                {
                    spellName = SpellName.DissonantWhispers,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Bard" },
                    diceCount = 3,
                    diceSize = 6
                }
            },
            {
                SpellName.DivineSmite, new SpellDefinition
                {
                    spellName = SpellName.DivineSmite,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Paladin" },
                    diceCount = 2,
                    diceSize = 8
                }
            },
            {
                SpellName.Entangle, new SpellDefinition
                {
                    spellName = SpellName.Entangle,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Druid", "Ranger" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.GuidingBolt, new SpellDefinition
                {
                    spellName = SpellName.GuidingBolt,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 4,
                    diceSize = 6
                }
            },
            {
                SpellName.HealingWord, new SpellDefinition
                {
                    spellName = SpellName.HealingWord,
                    spellType = SpellType.Healing,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Bard", "Cleric", "Druid" },
                    diceCount = 1,
                    diceSize = 4
                }
            },
            {
                SpellName.HellishRebuke, new SpellDefinition
                {
                    spellName = SpellName.HellishRebuke,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Warlock" },
                    diceCount = 2,
                    diceSize = 10
                }
            },
            {
                SpellName.HuntersMark, new SpellDefinition
                {
                    spellName = SpellName.HuntersMark,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Ranger" },
                    diceCount = 1,
                    diceSize = 6
                }
            },
            {
                SpellName.InflictWounds, new SpellDefinition
                {
                    spellName = SpellName.InflictWounds,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 3,
                    diceSize = 10
                }
            },
            {
                SpellName.MagicMissile, new SpellDefinition
                {
                    spellName = SpellName.MagicMissile,
                    spellType = SpellType.Damage,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 3,
                    diceSize = 4
                }
            },
            {
                SpellName.Shield, new SpellDefinition
                {
                    spellName = SpellName.Shield,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.ShieldOfFaith, new SpellDefinition
                {
                    spellName = SpellName.ShieldOfFaith,
                    spellType = SpellType.Utility,
                    spellLevel = 1,
                    allowedClasses = new List<string> { "Cleric", "Paladin" },
                    diceCount = 0,
                    diceSize = 0
                }
            },

            //=== 2ND LEVEL SPELLS ===
            {
                SpellName.Aid, new SpellDefinition
                {
                    spellName = SpellName.Aid,
                    spellType = SpellType.Utility,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Cleric", "Paladin", "Artificer" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.Darkness, new SpellDefinition
                {
                    spellName = SpellName.Darkness,
                    spellType = SpellType.Utility,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Sorcerer", "Warlock" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.HeatMetal, new SpellDefinition
                {
                    spellName = SpellName.HeatMetal,
                    spellType = SpellType.Damage,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Artificer", "Bard", "Druid" },
                    diceCount = 2,
                    diceSize = 8
                }
            },
            {
                SpellName.LesserRestoration, new SpellDefinition
                {
                    spellName = SpellName.LesserRestoration,
                    spellType = SpellType.Healing,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Bard", "Cleric", "Druid", "Paladin", "Ranger", "Artificer" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.MistyStep, new SpellDefinition
                {
                    spellName = SpellName.MistyStep,
                    spellType = SpellType.Utility,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Sorcerer", "Warlock", "Wizard" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.Moonbeam, new SpellDefinition
                {
                    spellName = SpellName.Moonbeam,
                    spellType = SpellType.Damage,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Druid" },
                    diceCount = 2,
                    diceSize = 10
                }
            },
            {
                SpellName.PrayerOfHealing, new SpellDefinition
                {
                    spellName = SpellName.PrayerOfHealing,
                    spellType = SpellType.Healing,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 2,
                    diceSize = 8
                }
            },
            {
                SpellName.ScorchingRay, new SpellDefinition
                {
                    spellName = SpellName.ScorchingRay,
                    spellType = SpellType.Damage,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 2,
                    diceSize = 6
                }
            },
            {
                SpellName.Shatter, new SpellDefinition
                {
                    spellName = SpellName.Shatter,
                    spellType = SpellType.Damage,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Bard", "Sorcerer", "Wizard" },
                    diceCount = 3,
                    diceSize = 8
                }
            },
            {
                SpellName.SpiritualWeapon, new SpellDefinition
                {
                    spellName = SpellName.SpiritualWeapon,
                    spellType = SpellType.Damage,
                    spellLevel = 2,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 1,
                    diceSize = 8
                }
            },

            //=== 3RD LEVEL SPELLS ===
            {
                SpellName.AuraOfVitality, new SpellDefinition
                {
                    spellName = SpellName.AuraOfVitality,
                    spellType = SpellType.Healing,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Cleric", "Druid", "Paladin" },
                    diceCount = 2,
                    diceSize = 6
                }
            },
            {
                SpellName.CallLightning, new SpellDefinition
                {
                    spellName = SpellName.CallLightning,
                    spellType = SpellType.Damage,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Druid" },
                    diceCount = 3,
                    diceSize = 10
                }
            },
            {
                SpellName.Counterspell, new SpellDefinition
                {
                    spellName = SpellName.Counterspell,
                    spellType = SpellType.Utility,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Sorcerer", "Warlock", "Wizard" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.Fireball, new SpellDefinition
                {
                    spellName = SpellName.Fireball,
                    spellType = SpellType.Damage,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 8,
                    diceSize = 6
                }
            },
            {
                SpellName.LightningBolt, new SpellDefinition
                {
                    spellName = SpellName.LightningBolt,
                    spellType = SpellType.Damage,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Sorcerer", "Wizard" },
                    diceCount = 8,
                    diceSize = 6
                }
            },
            {
                SpellName.MassHealingWord, new SpellDefinition
                {
                    spellName = SpellName.MassHealingWord,
                    spellType = SpellType.Healing,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Bard", "Cleric" },
                    diceCount = 1,
                    diceSize = 4
                }
            },
            {
                SpellName.Revivify, new SpellDefinition
                {
                    spellName = SpellName.Revivify,
                    spellType = SpellType.Healing,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Cleric", "Druid", "Paladin", "Ranger", "Artificer" },
                    diceCount = 0,
                    diceSize = 0
                }
            },
            {
                SpellName.SpiritGuardians, new SpellDefinition
                {
                    spellName = SpellName.SpiritGuardians,
                    spellType = SpellType.Damage,
                    spellLevel = 3,
                    allowedClasses = new List<string> { "Cleric" },
                    diceCount = 3,
                    diceSize = 8
                }
            }
        };
    }

    //Method to access data
    //Get a spell definition by its name
    //Returns null if the spell is not found
    public static SpellDefinition GetSpell(SpellName spellName)
    {
        return spells.TryGetValue(spellName, out var spell) ? spell : null;
    }

    //Getting a specific list of spells for a class
    public static List<SpellDefinition> GetSpellsForClass(string charClass)
    {
        return spells.Values
            .Where(spell => spell.allowedClasses.Contains(charClass))
            .ToList();
    }

    //Get all spells of a specific level available to a class
    public static List<SpellDefinition> GetSpellsForClassAtLevel(string charClass, int spellLevel)
    {
        return spells.Values
            .Where(spell => spell.allowedClasses.Contains(charClass) && spell.spellLevel == spellLevel)
            .ToList();
    }

    
    //Get all cantrips (level 0 spells) available to a class
    public static List<SpellDefinition> GetCantripsForClass(string charClass)
    {
        return GetSpellsForClassAtLevel(charClass, 0);
    }

    //Get all spells up to a certain level for a class (useful for level-gating).
    public static List<SpellDefinition> GetAvailableSpells(string charClass, int maxSpellLevel)
    {
        return spells.Values
            .Where(spell => spell.allowedClasses.Contains(charClass) && spell.spellLevel <= maxSpellLevel)
            .ToList();
    }

    //Check if a class can use a specific spell
    public static bool CanClassUseSpell(string charClass, SpellName spellName)
    {
        var spell = GetSpell(spellName);
        return spell != null && spell.allowedClasses.Contains(charClass);
    }
}

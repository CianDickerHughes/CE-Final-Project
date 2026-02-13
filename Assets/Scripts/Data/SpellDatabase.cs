using UnityEngine;
using System;
using System.Collections.Generic;


//This class is meant to be a sort of source for collecting information arount
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
        spells = new Dictionary<SpellName, SpellDefinition>();

        //Adding each spell to the db and their definition
        spells.Add(SpellName.AcidSplash, new SpellDefinition
        {
            spellName = SpellName.AcidSplash,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Wizard", "Sorcerer" },
            diceCount = 1,
            diceSize = 6
        });
        spells.Add(SpellName.BoomingBlade, new SpellDefinition
        {
            spellName = SpellName.BoomingBlade,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Warlock", "Sorcerer"},
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.CreateBonfire, new SpellDefinition
        {
            spellName = SpellName.CreateBonfire,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Druid", "Wizard" },
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.EldritchBlast, new SpellDefinition
        {
            spellName = SpellName.EldritchBlast,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Warlock" },
            diceCount = 1,
            diceSize = 10
        });
        spells.Add(SpellName.FaerieFire, new SpellDefinition
        {
            spellName = SpellName.FaerieFire,
            spellType = SpellType.Utility,
            spellLevel = 0,
            allowedClasses = new List<string> { "Druid", "Bard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.FireBolt, new SpellDefinition
        {
            spellName = SpellName.FireBolt,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 1,
            diceSize = 10
        });
        spells.Add(SpellName.RayOfFrost, new SpellDefinition
        {
            spellName = SpellName.RayOfFrost,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.SacredFlame, new SpellDefinition
        {
            spellName = SpellName.SacredFlame,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.SpareTheDying, new SpellDefinition
        {
            spellName = SpellName.SpareTheDying,
            spellType = SpellType.Healing,
            spellLevel = 0,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.ThornWhip, new SpellDefinition
        {
            spellName = SpellName.ThornWhip,
            spellType = SpellType.Damage,
            spellLevel = 0,
            allowedClasses = new List<string> { "Druid" },
            diceCount = 1,
            diceSize = 6
        });
        spells.Add(SpellName.TrueStrike, new SpellDefinition
        {
            spellName = SpellName.TrueStrike,
            spellType = SpellType.Utility,
            spellLevel = 0,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        //1st Level Spells
        spells.Add(SpellName.Bless, new SpellDefinition
        {
            spellName = SpellName.Bless,
            spellType = SpellType.Utility,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.BurningHands, new SpellDefinition
        {
            spellName = SpellName.BurningHands,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 3,
            diceSize = 6
        });
        spells.Add(SpellName.CureWounds, new SpellDefinition
        {
            spellName = SpellName.CureWounds,
            spellType = SpellType.Healing,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric", "Druid", "Bard" },
            diceCount = 1,
            diceSize = 8
        });
        spells.Add(SpellName.DivineSmite, new SpellDefinition
        {
            spellName = SpellName.DivineSmite,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Paladin" },
            diceCount = 2,
            diceSize = 8
        });
        spells.Add(SpellName.FalseLife, new SpellDefinition
        {
            spellName = SpellName.FalseLife,
            spellType = SpellType.Utility,
            spellLevel = 1,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 1,
            diceSize = 4
        });
        spells.Add(SpellName.GuidingBolt, new SpellDefinition
        {
            spellName = SpellName.GuidingBolt,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.HealingWord, new SpellDefinition
        {
            spellName = SpellName.HealingWord,
            spellType = SpellType.Healing,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric", "Bard" },
            diceCount = 1,
            diceSize = 4
        });
        spells.Add(SpellName.HellishRebuke, new SpellDefinition
        {
            spellName = SpellName.HellishRebuke,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Warlock" },
            diceCount = 2,
            diceSize = 10
        });
        spells.Add(SpellName.InflictWounds, new SpellDefinition
        {
            spellName = SpellName.InflictWounds,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 3,
            diceSize = 10
        });
        spells.Add(SpellName.MagicMissile, new SpellDefinition
        {
            spellName = SpellName.MagicMissile,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 3,
            diceSize = 4
        });
        spells.Add(SpellName.Shield, new SpellDefinition
        {
            spellName = SpellName.Shield,
            spellType = SpellType.Utility,
            spellLevel = 1,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.ShieldOfFaith, new SpellDefinition
        {
            spellName = SpellName.ShieldOfFaith,
            spellType = SpellType.Utility,
            spellLevel = 1,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.ThunderousSmite, new SpellDefinition
        {
            spellName = SpellName.ThunderousSmite,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Paladin" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.ThunderWave, new SpellDefinition
        {
            spellName = SpellName.ThunderWave,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 2,
            diceSize = 8
        });
        spells.Add(SpellName.WitchBolt, new SpellDefinition
        {
            spellName = SpellName.WitchBolt,
            spellType = SpellType.Damage,
            spellLevel = 1,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 1,
            diceSize = 12
        });
        //2nd Level Spells
        spells.Add(SpellName.Aid, new SpellDefinition
        {
            spellName = SpellName.Aid,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.Blur, new SpellDefinition
        {
            spellName = SpellName.Blur,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.DragonsBreath, new SpellDefinition
        {
            spellName = SpellName.DragonsBreath,
            spellType = SpellType.Damage,
            spellLevel = 2,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.FlamingSphere, new SpellDefinition
        {
            spellName = SpellName.FlamingSphere,
            spellType = SpellType.Damage,
            spellLevel = 2,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.GustOfWind, new SpellDefinition
        {
            spellName = SpellName.GustOfWind,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.HoldPerson, new SpellDefinition
        {
            spellName = SpellName.HoldPerson,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Cleric", "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.LesserRestoration, new SpellDefinition
        {
            spellName = SpellName.LesserRestoration,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Cleric", "Druid" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.MagicWeapon, new SpellDefinition
        {
            spellName = SpellName.MagicWeapon,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.MistyStep, new SpellDefinition
        {
            spellName = SpellName.MistyStep,
            spellType = SpellType.Utility,
            spellLevel = 2,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.Moonbeam, new SpellDefinition
        {
            spellName = SpellName.Moonbeam,
            spellType = SpellType.Damage,
            spellLevel = 2,
            allowedClasses = new List<string> { "Druid" },
            diceCount = 2,
            diceSize = 10
        });
        spells.Add(SpellName.PrayerOfHealing, new SpellDefinition
        {
            spellName = SpellName.PrayerOfHealing,
            spellType = SpellType.Healing,
            spellLevel = 2,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 2,
            diceSize = 8
        });
        spells.Add(SpellName.ScorchingRay, new SpellDefinition
        {
            spellName = SpellName.ScorchingRay,
            spellType = SpellType.Damage,
            spellLevel = 2,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.ShadowBlade, new SpellDefinition
        {
            spellName = SpellName.ShadowBlade,
            spellType = SpellType.Damage,
            spellLevel = 2,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 2,
            diceSize = 8
        });
        //3rd Level Spells
        spells.Add(SpellName.AuraOfVitality, new SpellDefinition
        {
            spellName = SpellName.AuraOfVitality,
            spellType = SpellType.Healing,
            spellLevel = 3,
            allowedClasses = new List<string> { "Paladin" },
            diceCount = 2,
            diceSize = 6
        });
        spells.Add(SpellName.CallLightning, new SpellDefinition
        {
            spellName = SpellName.CallLightning,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Druid" },
            diceCount = 3,
            diceSize = 10
        });
        spells.Add(SpellName.Counterspell, new SpellDefinition
        {
            spellName = SpellName.Counterspell,
            spellType = SpellType.Utility,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.ElementalWeapon, new SpellDefinition
        {
            spellName = SpellName.ElementalWeapon,
            spellType = SpellType.Utility,
            spellLevel = 3,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.FlameArrows, new SpellDefinition
        {
            spellName = SpellName.FlameArrows,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 3,
            diceSize = 6
        });
        spells.Add(SpellName.Fireball, new SpellDefinition
        {
            spellName = SpellName.Fireball,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 8,
            diceSize = 6
        });
        spells.Add(SpellName.Haste, new SpellDefinition
        {
            spellName = SpellName.Haste,
            spellType = SpellType.Utility,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.LightningBolt, new SpellDefinition
        {
            spellName = SpellName.LightningBolt,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 8,
            diceSize = 6
        });
        spells.Add(SpellName.MassHealingWord, new SpellDefinition
        {
            spellName = SpellName.MassHealingWord,
            spellType = SpellType.Healing,
            spellLevel = 3,
            allowedClasses = new List<string> { "Cleric", "Bard" },
            diceCount = 3,
            diceSize = 4
        });
        spells.Add(SpellName.PsionicBlast, new SpellDefinition
        {
            spellName = SpellName.PsionicBlast,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 8
        });
        spells.Add(SpellName.Revify, new SpellDefinition
        {
            spellName = SpellName.Revify,
            spellType = SpellType.Utility,
            spellLevel = 3,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.SpiritGuardians, new SpellDefinition
        {
            spellName = SpellName.SpiritGuardians,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 4,
            diceSize = 8
        });
        spells.Add(SpellName.VampiricTouch, new SpellDefinition
        {
            spellName = SpellName.VampiricTouch,
            spellType = SpellType.Damage,
            spellLevel = 3,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 3,
            diceSize = 6
        });
        //4th Level Spells
        spells.Add(SpellName.AuraOfLife, new SpellDefinition
        {
            spellName = SpellName.AuraOfLife,
            spellType = SpellType.Healing,
            spellLevel = 4,
            allowedClasses = new List<string> { "Paladin" },
            diceCount = 5,
            diceSize = 6
        });
        spells.Add(SpellName.Banishment, new SpellDefinition
        {
            spellName = SpellName.Banishment,
            spellType = SpellType.Utility,
            spellLevel = 4,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.FreedomOfMovement, new SpellDefinition
        {
            spellName = SpellName.FreedomOfMovement,
            spellType = SpellType.Utility,
            spellLevel = 4,
            allowedClasses = new List<string> { "Cleric", "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.StaggeringSmite, new SpellDefinition
        {
            spellName = SpellName.StaggeringSmite,
            spellType = SpellType.Damage,
            spellLevel = 4,
            allowedClasses = new List<string> { "Paladin" },
            diceCount = 4,
            diceSize = 6
        });
        spells.Add(SpellName.Stoneskin, new SpellDefinition
        {
            spellName = SpellName.Stoneskin,
            spellType = SpellType.Utility,
            spellLevel = 4,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.WallOfFire, new SpellDefinition
        {
            spellName = SpellName.WallOfFire,
            spellType = SpellType.Damage,
            spellLevel = 4,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 8
        });
        //5th Level Spells
        spells.Add(SpellName.DestructiveWave, new SpellDefinition
        {
            spellName = SpellName.DestructiveWave,
            spellType = SpellType.Damage,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 5,
            diceSize = 6
        });
        spells.Add(SpellName.GreaterRestoration, new SpellDefinition
        {
            spellName = SpellName.GreaterRestoration,
            spellType = SpellType.Utility,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Druid" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.HoldMonster, new SpellDefinition
        {
            spellName = SpellName.HoldMonster,
            spellType = SpellType.Utility,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.HolyWeapon, new SpellDefinition
        {
            spellName = SpellName.HolyWeapon,
            spellType = SpellType.Utility,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Paladin" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.FlameStrike, new SpellDefinition
        {
            spellName = SpellName.FlameStrike,
            spellType = SpellType.Damage,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Sorcerer", "Wizard" },
            diceCount = 4,
            diceSize = 10
        });
        spells.Add(SpellName.MassCureWounds, new SpellDefinition
        {
            spellName = SpellName.MassCureWounds,
            spellType = SpellType.Healing,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Druid", "Bard" },
            diceCount = 5,
            diceSize = 8
        });
        spells.Add(SpellName.SteelWindStrike, new SpellDefinition
        {
            spellName = SpellName.SteelWindStrike,
            spellType = SpellType.Damage,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Druid", "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 10
        });
        spells.Add(SpellName.WallOfStone, new SpellDefinition
        {
            spellName = SpellName.WallOfStone,
            spellType = SpellType.Utility,
            spellLevel = 5,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.WallOfLight, new SpellDefinition
        {
            spellName = SpellName.WallOfLight,
            spellType = SpellType.Damage,
            spellLevel = 5,
            allowedClasses = new List<string> { "Cleric", "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 8
        });
        //6th Level Spells
        spells.Add(SpellName.ChainLightning, new SpellDefinition
        {
            spellName = SpellName.ChainLightning,
            spellType = SpellType.Damage,
            spellLevel = 6,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 8
        });
        spells.Add(SpellName.Disintegrate, new SpellDefinition
        {
            spellName = SpellName.Disintegrate,
            spellType = SpellType.Damage,
            spellLevel = 6,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.Harm, new SpellDefinition
        {
            spellName = SpellName.Harm,
            spellType = SpellType.Damage,
            spellLevel = 6,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 14,
            diceSize = 6
        });
        spells.Add(SpellName.Heal, new SpellDefinition
        {
            spellName = SpellName.Heal,
            spellType = SpellType.Healing,
            spellLevel = 6,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.Sunbeam, new SpellDefinition
        {
            spellName = SpellName.Sunbeam,
            spellType = SpellType.Damage,
            spellLevel = 6,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 10
        });
        spells.Add(SpellName.WallOfIce, new SpellDefinition
        {
            spellName = SpellName.WallOfIce,
            spellType = SpellType.Utility,
            spellLevel = 6,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        //7th Level Spells
        spells.Add(SpellName.DivineWord, new SpellDefinition
        {
            spellName = SpellName.DivineWord,
            spellType = SpellType.Damage,
            spellLevel = 7,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 6,
            diceSize = 10
        });
        spells.Add(SpellName.FingerOfDeath, new SpellDefinition
        {
            spellName = SpellName.FingerOfDeath,
            spellType = SpellType.Damage,
            spellLevel = 7,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 7,
            diceSize = 8
        });
        spells.Add(SpellName.MordenkainensSword, new SpellDefinition
        {
            spellName = SpellName.MordenkainensSword,
            spellType = SpellType.Damage,
            spellLevel = 7,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 4,
            diceSize = 10
        });
        spells.Add(SpellName.PowerWordPain, new SpellDefinition
        {
            spellName = SpellName.PowerWordPain,
            spellType = SpellType.Damage,
            spellLevel = 7,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 8
        });
        spells.Add(SpellName.Regenerate, new SpellDefinition
        {
            spellName = SpellName.Regenerate,
            spellType = SpellType.Healing,
            spellLevel = 7,
            allowedClasses = new List<string> { "Cleric", "Druid" },
            diceCount = 8,
            diceSize = 8
        });
        spells.Add(SpellName.Resurrection, new SpellDefinition
        {
            spellName = SpellName.Resurrection,
            spellType = SpellType.Utility,
            spellLevel = 7,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.Whirlwind, new SpellDefinition
        {
            spellName = SpellName.Whirlwind,
            spellType = SpellType.Damage,
            spellLevel = 7,
            allowedClasses = new List<string> { "Druid" },
            diceCount = 6,
            diceSize = 10
        });
        //8th Level Spells
        spells.Add(SpellName.DominateMonster, new SpellDefinition
        {
            spellName = SpellName.DominateMonster,
            spellType = SpellType.Utility,
            spellLevel = 8,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.Earthquake, new SpellDefinition
        {
            spellName = SpellName.Earthquake,
            spellType = SpellType.Damage,
            spellLevel = 8,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.PowerWordStun, new SpellDefinition
        {
            spellName = SpellName.PowerWordStun,
            spellType = SpellType.Damage,
            spellLevel = 8,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 6,
            diceSize = 8
        });
        spells.Add(SpellName.Sunburst, new SpellDefinition
        {
            spellName = SpellName.Sunburst,
            spellType = SpellType.Damage,
            spellLevel = 8,
            allowedClasses = new List<string> { "Druid", "Sorcerer", "Wizard" },
            diceCount = 12,
            diceSize = 6
        });
        //9th Level Spells
        spells.Add(SpellName.BladeOfDisaster, new SpellDefinition
        {
            spellName = SpellName.BladeOfDisaster,
            spellType = SpellType.Damage,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 10
        });
        spells.Add(SpellName.Invulnerability, new SpellDefinition
        {
            spellName = SpellName.Invulnerability,
            spellType = SpellType.Utility,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.MassHeal, new SpellDefinition
        {
            spellName = SpellName.MassHeal,
            spellType = SpellType.Healing,
            spellLevel = 9,
            allowedClasses = new List<string> { "Cleric", "Druid", "Bard" },
            diceCount = 12,
            diceSize = 8
        });
        spells.Add(SpellName.PowerWordHeal, new SpellDefinition
        {
            spellName = SpellName.PowerWordHeal,
            spellType = SpellType.Healing,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.PowerWordKill, new SpellDefinition
        {
            spellName = SpellName.PowerWordKill,
            spellType = SpellType.Damage,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.PsychicScream, new SpellDefinition
        {
            spellName = SpellName.PsychicScream,
            spellType = SpellType.Damage,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 10,
            diceSize = 6
        });
        spells.Add(SpellName.PrismaticWall, new SpellDefinition
        {
            spellName = SpellName.PrismaticWall,
            spellType = SpellType.Utility,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.TrueResurrection, new SpellDefinition
        {
            spellName = SpellName.TrueResurrection,
            spellType = SpellType.Utility,
            spellLevel = 9,
            allowedClasses = new List<string> { "Cleric" },
            diceCount = 0,
            diceSize = 0
        });
        spells.Add(SpellName.Wish, new SpellDefinition
        {
            spellName = SpellName.Wish,
            spellType = SpellType.Utility,
            spellLevel = 9,
            allowedClasses = new List<string> { "Sorcerer", "Wizard" },
            diceCount = 0,
            diceSize = 0
        });
    }

    //Methods to access data
    public static SpellDefinition GetSpell(SpellName spellName)
    {
        //Returning the spell - handling it if not found
        return spells.ContainsKey(spellName) ? spells[spellName] : null;
    }

    //Getting a specific list of spells for a class
    public static List<SpellDefinition> GetSpellsForClass(string charClass)
    {
        //Filtering spells based on the class passed
        return new List<SpellDefinition>(spells.Values).FindAll(spell => spell.allowedClasses.Contains(charClass));
    }
}

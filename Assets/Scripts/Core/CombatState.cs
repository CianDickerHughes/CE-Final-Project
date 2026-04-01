using UnityEngine;

//Defnining the state that combat can be in at any given point in time
public enum CombatState
{
    //Combat hasnt started/has ended
    Inactive,
    //Rolling phase - setting things up
    Rolling,
    //Active combat phase - players and npcs taking actions
    Active,
    //Paused - for things like when the DM needs to pause combat to do something or if a player needs to step away
    Paused
}

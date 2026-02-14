using UnityEngine;
using System.Collections.Generic;
using System;

//This class is meant to serve as a manager for the combat related behaviours in the gameplay scene
// - Handling turns, initiative, combat actions, etc
public class CombatManager : MonoBehaviour
{
    //Need to track who is participating in combat, both players and NPCs
    public List<Token> combatParticipant;
    //List of combat participants with their initiative rolls and whether they've acted this round or not
    public List<CombatParticipant> initiativeOrder;
    //Need an instance of the gameplay manager to access spawned tokens
    private GameplayManager gameplayManager;

    //Turn management
    private int currentTurnIndex = 0;
    private CombatState combatState = CombatState.Inactive;

    //DM Controls
    [SerializeField] private GameObject startCombatButton;
    [SerializeField] private GameObject pauseCombatButton;
    [SerializeField] private GameObject endCombatButton;
    [SerializeField] private GameObject nextTurnButton;

    void Start()
    {
        gameplayManager = GameplayManager.Instance;
    }

    //Method to populate the list of combatants with the spawned tokens from the gameplay manager
    public void PopulateCombatList(){
        combatParticipant = new List<Token>(gameplayManager.GetSpawnedTokens());
    }

    //DM Control methods
    public void startCombat()
    {
        if(combatState == CombatState.Inactive)
        {
            combatState = CombatState.Rolling;
            RollInitiative();
            //Update UI to show initiative order, enable turn controls, etc
        }
    }

    public void pauseCombat()
    {
        if(combatState == CombatState.Active)
        {
            combatState = CombatState.Paused;
            //Update UI to reflect paused state, disable turn controls, etc
        }
    }

    public void endCombat()
    {
        combatState = CombatState.Inactive;
        currentTurnIndex = 0;
        initiativeOrder.Clear();
        //Update UI to reflect end of combat, disable turn controls, etc
    }

    public void nextTurn()
    {
        if(combatState == CombatState.Active)
        {
            currentTurnIndex = (currentTurnIndex + 1) % initiativeOrder.Count;
            //Update UI to highlight current turn, reset action states for the new turn, etc
        }
    }

    //Combat Setup methods
    private void RollInitiative()
    {
        initiativeOrder = new List<CombatParticipant>();
        foreach(var participant in combatParticipant)
        {
            CombatParticipant participant = new CombatParticipant
            {
                token = participant,
                initiativeRoll = UnityEngine.Random.Range(1, 21),
                hasActedThisRound = false
            };
            initiativeOrder.Add(participant);
        }
        //Sort the initiative order based on the rolls (descending)
        initiativeOrder.Sort((a, b) => b.initiativeRoll.CompareTo(a.initiativeRoll));
        combatState = CombatState.Active;
    }
}

[Serializable]
public struct CombatParticipant
{
    public Token token;
    public int initiativeRoll;
    public bool hasActedThisRound;
    public int currentHP;
    public int maxHP;
}
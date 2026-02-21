using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Button startCombatButton;
    [SerializeField] private Button pauseCombatButton;
    [SerializeField] private TextMeshProUGUI pausedButtonText;
    [SerializeField] private Button endCombatButton;
    [SerializeField] private Button nextTurnButton;
    //UI Elements for the characters in combat - like the initiative tracker in bg3
    [SerializeField] private Transform combatantsUIParent;
    [SerializeField] private GameObject combatantUIPrefab;
    //Meant for visually tracking what the current state of combat is
    [SerializeField] private TextMeshProUGUI combatStateText;

    void Start()
    {
        gameplayManager = GameplayManager.Instance;

        //Initializing the lists
        combatParticipant = new List<Token>();
        initiativeOrder = new List<CombatParticipant>();
        
        //Wiring up the buttons for controlling combat
        if (startCombatButton != null)
        {
            startCombatButton.onClick.AddListener(startCombat);
        } 
        if (pauseCombatButton != null)
        {
            pauseCombatButton.onClick.AddListener(pauseCombat);
        }
        if (endCombatButton != null)
        {
            endCombatButton.onClick.AddListener(endCombat);
        }
        if (nextTurnButton != null)
        {
            nextTurnButton.onClick.AddListener(nextTurn);
        }
    }

    //Method to populate the list of combatants with the spawned tokens from the gameplay manager
    public void PopulateCombatList(){
        combatParticipant = new List<Token>(TokenManager.Instance.GetSpawnedTokens());
    }

    //DM Control methods
    public void startCombat()
    {
        if (combatState == CombatState.Inactive) {
            PopulateCombatList(); 
            combatState = CombatState.Rolling;
            RollInitiative();
            //Updating the UI reference to reflect combat starting
            if(combatStateText != null)
            {
                combatStateText.text = "Combat Started!";
            }
        }
    }

    public void pauseCombat()
    {
        if(combatState == CombatState.Active && combatState != CombatState.Paused)
        {
            combatState = CombatState.Paused;
            //Update UI to reflect paused state, disable turn controls, etc
            if(combatStateText != null)
            {
                combatStateText.text = "Combat Paused!";
                //Changing the UI of the button to reflect the paused state
                pausedButtonText.text = "Resume Combat";
            }
        }
        else if(combatState == CombatState.Paused)
        {
            combatState = CombatState.Active;
            //Updating the UI to reflect combat actually resuming
            if(combatStateText != null)
            {
                combatStateText.text = "Combat Resumed!";
                pausedButtonText.text = "Pause Combat";
            }
        }
    }

    public void endCombat()
    {
        combatState = CombatState.Inactive;
        currentTurnIndex = 0;
        initiativeOrder.Clear();
        //Update UI to reflect end of combat, disable turn controls, etc
        if(combatStateText != null)
        {
            combatStateText.text = "Combat Ended!";
        }

        //Clearing the combatants UI
        foreach(Transform child in combatantsUIParent)
        {
            Destroy(child.gameObject);
        }
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
            int dexMod = (participant.getCharacterType() == CharacterType.Enemy)
                ? (participant.getEnemyData().dexterity - 10) / 2
                : (participant.getCharacterData().dexterity - 10) / 2;

            int maxHPValue = (participant.getCharacterType() == CharacterType.Enemy)
                ? participant.getEnemyData().HP
                : participant.getCharacterData().HP;

            CombatParticipant combatant = new CombatParticipant
            {
                token = participant,
                initiativeRoll = UnityEngine.Random.Range(1, 21) + dexMod,
                hasActedThisRound = false,
                maxHP = maxHPValue,
                currentHP = maxHPValue  // Start at full health
            };
            initiativeOrder.Add(combatant);
        }
        //Sort the initiative order based on the rolls (descending)
        initiativeOrder.Sort((a, b) => b.initiativeRoll.CompareTo(a.initiativeRoll));
        combatState = CombatState.Active;
        populateCharactersUI();
    }

    //UTILITY METHODS
    public int GetDexModifier(Token token)
    {
        int dex = (token.getCharacterType() == CharacterType.Enemy)
            ? token.getEnemyData().dexterity
            : token.getCharacterData().dexterity;
        return (dex - 10) / 2;
    }

    //COMBAT STATE REFLECTION METHODS
    //Attacking another token/character
    public void ApplyDamage(int participantIndex, int damage)
    {
        if (participantIndex < 0 || participantIndex >= initiativeOrder.Count)
        {
            return;
        }
    
        CombatParticipant target = initiativeOrder[participantIndex];
        target.currentHP = Mathf.Max(0, target.currentHP - damage);
        initiativeOrder[participantIndex] = target;  // Structs require reassignment
        
        Debug.Log($"{target.GetName()} took {damage} damage. HP: {target.currentHP}/{target.maxHP}");
    }

    //Healing a token/character
    public void ApplyHealing(int participantIndex, int healing) {
        if (participantIndex < 0 || participantIndex >= initiativeOrder.Count) 
        {
            return;
        }
        
        CombatParticipant target = initiativeOrder[participantIndex];
        target.currentHP = Mathf.Min(target.maxHP, target.currentHP + healing);
        initiativeOrder[participantIndex] = target;
        
        Debug.Log($"{target.GetName()} healed for {healing}. HP: {target.currentHP}/{target.maxHP}");
    }

    public CombatParticipant GetCurrentTurnParticipant() {
        if (initiativeOrder == null || initiativeOrder.Count == 0)
            return default;
        return initiativeOrder[currentTurnIndex];
    }

    public CombatState GetCombatState() {
        return combatState;
    }

    public List<CombatParticipant> GetInitiativeOrder() {
        return initiativeOrder;
    }

    public int GetCurrentTurnIndex() {
        return currentTurnIndex;
    }

    //Method for populating the simple UI feature in the header with the combatants tokens - like bg3
    public void populateCharactersUI()
    {
        //Clear all existing UI elements
        foreach(Transform child in combatantsUIParent)
        {
            Destroy(child.gameObject);
        }
        //Loop through the initiative order and create UI elements for each combatant
        foreach(var combatant in initiativeOrder)
        {
            //Logic would go here
            //Populate UI with combatant.token's image, name, HP, etc
            GameObject uiElement = Instantiate(combatantUIPrefab, combatantsUIParent);
            CharInitToken charInitToken = uiElement.GetComponent<CharInitToken>();
            if(charInitToken != null)
            {
                charInitToken.Setup(combatant);
            }
        }
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

    //Getting AC
    public int GetAC(){
        if(token.getCharacterType() == CharacterType.Player)
        {
            return token.getCharacterData().AC;
        }
        return token.getEnemyData().AC;
    }

    //Getting name
    public string GetName()
    {
        if(token.getCharacterType() == CharacterType.Enemy)
        {
            return token.getEnemyData().name;
        }
        return token.getCharacterData().charName;
    }

    //Getting speed
    public int GetSpeed()
    {
        if(token.getCharacterType() == CharacterType.Player)
        {
            return token.getCharacterData().speed;
        }
        return token.getEnemyData().speed;
    }

    //Getting initative modifier
    public int GetInitiativeModifier()
    {
        if(token.getCharacterType() == CharacterType.Player)
        {
            return token.getCharacterData().dexterity;
        }
        return token.getEnemyData().dexterity;
    }
}
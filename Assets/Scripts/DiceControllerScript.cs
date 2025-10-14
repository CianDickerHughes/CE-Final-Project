using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random=UnityEngine.Random;

public class DiceControllerScript : MonoBehaviour
{
    //Global Variables
    public Button[] diceButtons; //Array for representing all potential dice
    public int[] diceSides; //Array for representing the sides of the individual dice
    public InputField numDice; //Variable for how many of a particular dice will be rolled
    public InputField totalModifier; //Meant to represent the total modifier to be applied to the result e.g. +3, -2, 0
    public Button rollButton; //Variable for checking/verifying when the roll button is clicked
    public Text resultOfRoll; //Simplistic text to be returned that will be updated with the result of the roll
    private int selectedDiceType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

     // Call this from your dice button's OnClick event, passing the dice type (e.g., 4, 6, 8, etc.)
    public void OnDiceButtonClicked(int diceType)
    {
        selectedDiceType = diceType;
        Debug.Log($"Dice button clicked! D{diceType} was selected.");
    }

    // Call this from your Roll Dice button's OnClick event
    public void OnRollDiceButtonClicked()
    {
        if (selectedDiceType == 0)
        {
            Debug.Log("No dice selected!");
            return;
        }
        int result = RollDice(selectedDiceType, 1, 0); // Rolls 1 dice, modifier 0
        Debug.Log($"Rolled D{selectedDiceType}: Result = {result}");
    }


    // Method to roll dice
    public int RollDice(int diceType, int diceCount, int modifier)
    {
        int total = 0;

        // Roll each dice and add the result
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, diceType + 1); // Random.Range is inclusive of min and exclusive of max, so add 1
        }

        // Add the modifier
        total += modifier;

        return total;
    }
}

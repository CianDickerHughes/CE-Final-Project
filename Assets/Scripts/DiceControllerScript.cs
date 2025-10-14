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

    //Variables for coloring the buttons after a click is performed - show user what they've done
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.6f, 0.9f, 1f);

    //Private Variables - helping to control behaviour
    private int selectedSides = 0;
    public int selectedDiceType = 0;
    private Button currentlyHighlighted = null;

    //Awake is kind of like the constructor for Unity - used to initialize things not dependent on other stuff before the application starts
    void Awake(){
        //Making sure nothing is set when we initially start up the application
        if (diceButtons != null && diceSides != null && diceButtons.Length != diceSides.Length)
        {
            Debug.LogWarning("diceButtons and diceSides should have the same length and order.");
        }

        //This is basically a way for us to prevent a crash if one of the arrays for buttons & sides arent assigned - we get a default of 0
        int count = Mathf.Min(diceButtons?.Length ?? 0, diceSides?.Length ?? 0);
        for (int i = 0; i < count; i++)
        {
            //Initializing the button & its sides
            int sides = diceSides[i];
            Button b = diceButtons[i];
            if (b == null) 
            {
                continue;
            }

            //Adding a transition for each button - basically allows us to select & change their state/look while running
            b.transition = Selectable.Transition.None;
            //Adding an event listener to each button with the appropriate method & their sides
            b.onClick.AddListener(() => OnDiceButtonClicked(b, sides));

            //Setting up the colour changes - ensuring they are all set to their normal white if not selected
            var img = b.GetComponent<Image>();
            if (img != null)
            {
                img.color = normalColor;
            }
        }

        //Initializing the roll button with the appropriate event listener
        if (rollButton != null)
        {
            rollButton.onClick.AddListener(OnRollButtonClicked);
        }

        //Initializing the result being displayed to the player
        if (resultOfRoll != null) 
        {
            resultOfRoll.text = "0";
        }
    }

    //Method fo controlling when the dice button is clicked - passing the specific button & the respective sides
    private void OnDiceButtonClicked(Button b, int sides)
    {
        //Initializing the selected sides & changing the colour of the button
        selectedSides = sides;
        SetHighlight(b);

        //Returning a message to the player to notify them of their choice - potentially remove this in later iterations
        if (resultOfRoll != null)
        {
            resultOfRoll.text = $"Selected: d{selectedSides}";
        }
    }

    //Method for setting the higlight/colour of the button we click - simply to notify the user their change has taken place
    private void SetHighlight(Button clicked)
    {
        //This chunck just "deselects" or un-highlights the previous button so we can highlight the properly chosen one - better user experience
        if (currentlyHighlighted != null)
        {
            var prevImg = currentlyHighlighted.GetComponent<Image>();
            if (prevImg != null)
            {
                prevImg.color = normalColor;
            }
        }

        //And this chunck highlights the new button the user has just checked
        currentlyHighlighted = clicked;
        if (currentlyHighlighted != null)
        {
            var img = currentlyHighlighted.GetComponent<Image>();
            if (img != null)
            {
                img.color = highlightColor;
            }
        }
    }

    //Method for handling the roll button being clicked
    private void OnRollButtonClicked()
    {
        //Error handling - making sure the user has actually selected a dice
        if (selectedSides <= 0)
        {
            if (resultOfRoll != null) resultOfRoll.text = "Please select a dice type first.";
            return;
        }

        //Parsing the Quantity & Modifier to be ints - so we can actually use them
        int qty = ParseIntField(numDice, 1);
        //This just makes sure that the value is between 1-100 -> we only do this for quantity since you have to roll at least one dice
        qty = Mathf.Clamp(qty, 1, 100);
        //We dont clamp this since the modifieres can be negitive or positive
        int modifier = ParseIntField(totalModifier, 0);

        // Backend returns a simple integer total
        int total = 0;
        for (int i = 0; i < qty; i++)
        {
            total += Random.Range(1, selectedSides + 1);
        }
        total += modifier;

        //Actually returning the final result of the roll to the user
        if (resultOfRoll != null)
        {
            resultOfRoll.text = total.ToString();
        }
    }

    //Method for parsing the inputs - needed for number of dice & modifier e.g. "+3"
    //Fallback is a default value we pass to fall back on
    private int ParseIntField(InputField f, int fallback)
    {
        if (f == null) 
        {
            return fallback;
        }

        //Trimming any empty space
        string t = f.text?.Trim();
        
        if(string.IsNullOrEmpty(t))
        {
            return fallback;
        }

        if (int.TryParse(t, out int v))
        {
            return v;
        }
        //Removing the + or - from the value entered & returning the actual parsed value
        if (t.StartsWith("+") || t.StartsWith("-") && int.TryParse(t.Substring(1), out v)) 
        {
            return v;
        }
        return fallback;
    }

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

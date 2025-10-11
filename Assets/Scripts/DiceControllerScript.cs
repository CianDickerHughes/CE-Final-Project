using UnityEngine;

public class DiceControllerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

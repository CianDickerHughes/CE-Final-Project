using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

//Same as the other classes we've seen
//This is meant to higlight the information about enemy items in the game
public class EnemyItem : MonoBehaviour
{
    //Variables for UI References
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyTypeText;
    public TextMeshProUGUI enemyChallengeRatingText;
    public Image enemyIconImage;
    //Same spawn functionalities as we have for characters
    public Button spawnButton;
    private string filePath;

    //Setup method to initialize the enemy item with data
    void SetUp(){
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

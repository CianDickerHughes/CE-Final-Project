using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This UI is shown FIRST when a player wants to join a campaign
//Player enters join code and username, connects via netcode, THEN selects character

//NOTE - A LOT OF THESE PIECES ARE PLACEHOLDERS FOR NOW UNTIL NETCODE IS FULLY IMPLEMENTED/ WONT WORK PROPERLY AT ALL YET

//CLASS WILL MOST LIKELY BE SCRAPPED OR CHANGED NOW THAT WE HAVE NETCODE WORKING NEARLY COMPLETELY

public class CampaignJoinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("References")]
    [SerializeField] private CharacterSelector characterSelector;
    
    void Start()
    {
        joinButton.onClick.AddListener(OnJoinButtonClicked);
    }
    
    //When player clicks the "Join" button
    private void OnJoinButtonClicked()
    {
        string joinCode = joinCodeInput.text.Trim().ToUpper();
        string username = usernameInput.text.Trim();
        
        //Validation
        if (string.IsNullOrEmpty(joinCode))
        {
            statusText.text = "Please enter a join code";
            return;
        }
        
        if (string.IsNullOrEmpty(username))
        {
            statusText.text = "Please enter a username";
            return;
        }
        
        //Store username for later use
        PlayerPrefs.SetString("PlayerUsername", username);
        PlayerPrefs.Save();
        
        statusText.text = "Connecting...";
        joinButton.interactable = false;
        
        //NOTE : The actual network connection code would go here
        //Initiate network connection via Unity Netcode
        //This is where we would call our NetworkManager to join as a client
        //NetworkConnectionManager.Instance?.JoinGame(joinCode, OnConnectionSuccess, OnConnectionFailed);
    }
    
    //Called when successfully connected to the host
    private void OnConnectionSuccess()
    {
        statusText.text = "Connected! Select your character...";
        
        //Hide this join UI
        gameObject.SetActive(false);
        
        //Show the character selection UI
        characterSelector.ShowCharacterSelection();
    }
    
    //Called if connection failed
    private void OnConnectionFailed(string errorMessage)
    {
        statusText.text = $"Connection failed: {errorMessage}";
        joinButton.interactable = true;
    }
}
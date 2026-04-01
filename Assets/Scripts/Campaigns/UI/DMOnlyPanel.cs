using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;

public class DMOnlyPanel : MonoBehaviour
{
    //Declaring the variable for the DM only panel
    [SerializeField] private GameObject dmPanel;

    //Checking if the player is the dm and if not hiding the panel
    void Start()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if(networkManager != null && networkManager.IsServer)
        {
            //Player is the DM, show the panel
            dmPanel.SetActive(true);
        }
        else
        {
            //Player isnt the DM, hide the dm panel
            dmPanel.SetActive(false);
        }
    }
}

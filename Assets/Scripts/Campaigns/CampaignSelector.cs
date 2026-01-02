using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This script manages the campaign selection UI for the DM/User who is creating a new campaign
//It lists all campaigns created by the current user and allows selection
public class CampaignSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform campaignListContainer;
    [SerializeField] private GameObject campaignItemPrefab;

    private List<Campaign> userCampaigns = new List<Campaign>();

    //To start we load in all campaigns created by the current user and populate the list
    void Start()
    {
        LoadUserCampaigns();
        PopulateCampaignList();
    }

    //Loads campaigns from Assets/Campaigns and filters by current user
    private void LoadUserCampaigns()
    {
        userCampaigns.Clear();
        string campaignsPath = Path.Combine(Application.dataPath, "Campaigns");
        if (!Directory.Exists(campaignsPath)) return;
        string[] files = Directory.GetFiles(campaignsPath, "*.json");
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            Campaign campaign = JsonUtility.FromJson<Campaign>(json);
            if (campaign != null && campaign.dmUsername == SessionManager.Instance.CurrentUsername)
            {
                userCampaigns.Add(campaign);
            }
        }
    }

    //Populates the scroll view with campaign items
    //This will soon be changed to being specifically the user's campaigns
    private void PopulateCampaignList()
    {
        foreach (Transform child in campaignListContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Campaign campaign in userCampaigns)
        {
            //Instantiate campaign item prefab - this just has a script to set up its own UI
            GameObject item = Instantiate(campaignItemPrefab, campaignListContainer);
            CampaignItem itemScript = item.GetComponent<CampaignItem>();
            //If the item isnt null i.e. has the script, set it up
            if (itemScript != null)
            {
                //We don't need the file path here, so passing null
                //All this does is set up the UI elements for the loaded campaigns in the list
                itemScript.SetCampaign(campaign, null);
            }
        }
    }

    //Call this after creating a new campaign to refresh the list
    public void RefreshCampaignList()
    {
        LoadUserCampaigns();
        PopulateCampaignList();
    }
}
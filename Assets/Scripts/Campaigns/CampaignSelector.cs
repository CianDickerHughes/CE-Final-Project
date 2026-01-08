using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

//This script manages the campaign selection UI for the DM/User who is creating a new campaign
//It lists all campaigns created by the current user and allows selection
public class CampaignSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform campaignListContainer;
    [SerializeField] private GameObject campaignItemPrefab;
    [SerializeField] private TextMeshProUGUI emptyText; // Optional: show when no campaigns

    //Store campaigns with their file paths for deletion
    private List<(Campaign campaign, string filePath)> userCampaigns = new List<(Campaign, string)>();

    //To start we load in all campaigns created by the current user and populate the list
    void Start()
    {
        LoadUserCampaigns();
        PopulateCampaignList();
    }

    //Loads campaigns from the Campaigns folder and filters by current user
    //Now searches through campaign subfolders for the .json files
    private void LoadUserCampaigns()
    {
        userCampaigns.Clear();
        string campaignsPath = CampaignManager.GetCampaignsFolder();
        if (!Directory.Exists(campaignsPath)) return;
        
        //Search through each campaign subfolder
        string[] campaignFolders = Directory.GetDirectories(campaignsPath);
        foreach (string folder in campaignFolders)
        {
            string[] files = Directory.GetFiles(folder, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    Campaign campaign = JsonUtility.FromJson<Campaign>(json);
                    if (campaign != null && campaign.dmUsername == SessionManager.Instance.CurrentUsername)
                    {
                        userCampaigns.Add((campaign, file));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to load campaign from {file}: {ex.Message}");
                }
            }
        }
    }

    //Populates the scroll view with campaign items
    private void PopulateCampaignList()
    {
        //Clear existing items
        foreach (Transform child in campaignListContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Show empty text if no campaigns
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(userCampaigns.Count == 0);
        }
        
        //Create an item for each campaign
        foreach (var (campaign, filePath) in userCampaigns)
        {
            GameObject item = Instantiate(campaignItemPrefab, campaignListContainer);
            CampaignItem itemScript = item.GetComponent<CampaignItem>();
            if (itemScript != null)
            {
                //Pass the campaign data and callbacks for Play/Delete
                itemScript.SetCampaign(campaign, filePath, OnPlayCampaign, OnDeleteCampaign);
            }
        }
    }

    //Called when user clicks Play on a campaign
    private void OnPlayCampaign(Campaign campaign, string filePath)
    {
        //Set the selection context so the CampaignManager scene knows which campaign to load
        CampaignSelectionContext.SelectedCampaignId = campaign.campaignId;
        CampaignSelectionContext.SelectedCampaignFilePath = filePath;
        
        Debug.Log($"Loading campaign: {campaign.campaignName}");
        
        //Load the CampaignManager scene
        SceneManager.LoadScene("CampaignManager");
    }
    
    //Called when user clicks Delete on a campaign
    private void OnDeleteCampaign(Campaign campaign, string filePath)
    {
        //Delete the entire campaign folder (which contains the JSON and any assets like the logo)
        if (File.Exists(filePath))
        {
            try
            {
                //Get the campaign folder path (parent of the JSON file)
                string campaignFolder = Path.GetDirectoryName(filePath);
                
                //Delete the entire campaign folder and its contents
                if (Directory.Exists(campaignFolder))
                {
                    Directory.Delete(campaignFolder, true); //true = recursive delete
                    Debug.Log($"Deleted campaign folder: {campaign.campaignName}");
                }
                
                //Refresh the list
                RefreshCampaignList();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete campaign: {ex.Message}");
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
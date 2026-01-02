using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach this script to your Campaigns Page Main Panel
public class CampaignSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform campaignListContainer;
    [SerializeField] private GameObject campaignItemPrefab;

    private List<Campaign> userCampaigns = new List<Campaign>();

    void Start()
    {
        LoadUserCampaigns();
        PopulateCampaignList();
    }

    // Loads campaigns from Assets/Campaigns and filters by current user
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

    // Populates the scroll view with campaign items
    private void PopulateCampaignList()
    {
        foreach (Transform child in campaignListContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Campaign campaign in userCampaigns)
        {
            GameObject item = Instantiate(campaignItemPrefab, campaignListContainer);
            CampaignItemUI itemUI = item.GetComponent<CampaignItemUI>();
            if (itemUI != null)
            {
                itemUI.SetCampaign(campaign);
            }
        }
    }

    // Call this after creating a new campaign to refresh the list
    public void RefreshCampaignList()
    {
        LoadUserCampaigns();
        PopulateCampaignList();
    }
}

// This is a helper script for the campaign item prefab
// Attach this to your campaign item prefab
public class CampaignItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image logoImage;

    private Campaign campaignData;

    public void SetCampaign(Campaign campaign)
    {
        campaignData = campaign;
        nameText.text = campaign.campaignName;
        descriptionText.text = campaign.campaignDescription;
        if (!string.IsNullOrEmpty(campaign.campaignLogoPath) && File.Exists(campaign.campaignLogoPath))
        {
            byte[] bytes = File.ReadAllBytes(campaign.campaignLogoPath);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                logoImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                logoImage.preserveAspect = true;
            }
        }
    }

    // Add button click handler here to open campaign manager, if needed
}

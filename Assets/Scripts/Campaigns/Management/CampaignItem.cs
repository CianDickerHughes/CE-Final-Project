using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CampaignItem : MonoBehaviour
{
    //UI References for variables
    public Image campaignLogoImage;
    public TextMeshProUGUI campaignNameText;
    public TextMeshProUGUI campaignDescriptionText;
    public Button selectButton;
    private Campaign campaignData;
    private string campaignFilePath;

    //Set up the campaign item with data
    public void SetCampaign(Campaign campaign, string filePath){
        campaignData = campaign;
        campaignFilePath = filePath;

        if (campaignNameText != null)
            campaignNameText.text = campaign.campaignName;

        if (campaignDescriptionText != null)
            campaignDescriptionText.text = campaign.campaignDescription;

        //Load logo if available
        //Previously this was just a literal reference to the logo path on the persons device
        //Now we copy the logo to the Campaigns folder and just store the file name
        if (campaignLogoImage != null && !string.IsNullOrEmpty(campaign.campaignLogoPath))
        {
            string logoPath = System.IO.Path.Combine(CampaignManager.GetCampaignsFolder(), campaign.campaignLogoPath);
            if (System.IO.File.Exists(logoPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(logoPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    campaignLogoImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    campaignLogoImage.preserveAspect = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load campaign logo: {ex.Message}");
                }
            }
        }

        //Setup button listener
        //Not 100% what i want to do with this yet but have it here in case
        if (selectButton != null)
        {
            //selectButton.onClick.RemoveAllListeners();
            //selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }
}

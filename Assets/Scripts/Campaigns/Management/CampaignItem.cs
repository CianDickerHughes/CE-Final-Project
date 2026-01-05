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
    public Button playButton;      // Renamed from selectButton for clarity
    public Button deleteButton;    // New delete button
    
    private Campaign campaignData;
    private string campaignFilePath;
    
    //Callbacks for button actions - set by CampaignSelector
    private Action<Campaign, string> onPlayClicked;
    private Action<Campaign, string> onDeleteClicked;

    //Set up the campaign item with data and callbacks
    public void SetCampaign(Campaign campaign, string filePath, Action<Campaign, string> onPlay = null, Action<Campaign, string> onDelete = null)
    {
        campaignData = campaign;
        campaignFilePath = filePath;
        onPlayClicked = onPlay;
        onDeleteClicked = onDelete;

        if (campaignNameText != null)
            campaignNameText.text = campaign.campaignName;

        if (campaignDescriptionText != null)
            campaignDescriptionText.text = campaign.campaignDescription;

        //Load logo if available
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

        //Wire up the Play button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        
        //Wire up the Delete button
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }
    
    //Called when the Play button is clicked
    private void OnPlayButtonClicked()
    {
        onPlayClicked?.Invoke(campaignData, campaignFilePath);
    }
    
    //Called when the Delete button is clicked
    private void OnDeleteButtonClicked()
    {
        onDeleteClicked?.Invoke(campaignData, campaignFilePath);
    }
}

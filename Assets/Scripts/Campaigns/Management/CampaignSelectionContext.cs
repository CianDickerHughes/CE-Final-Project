using UnityEngine;

//Static context class to pass the selected campaign data between scenes
//Works just like CharacterSelectionContext - stores the campaign ID and file path
//so the CampaignManager scene knows which campaign to load and display

public static class CampaignSelectionContext
{
    //The campaign ID of the selected campaign
    public static string SelectedCampaignId = null;
    
    //The file path to the campaign JSON (useful for saving/deleting)
    public static string SelectedCampaignFilePath = null;
    
    //Clear the selection (call when going back or creating new)
    public static void Clear()
    {
        SelectedCampaignId = null;
        SelectedCampaignFilePath = null;
    }
    
    //Check if a campaign is selected
    public static bool HasSelection => !string.IsNullOrEmpty(SelectedCampaignId);
}

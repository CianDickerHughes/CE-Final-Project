using System.Collections.Generic;
using UnityEngine;

//This class will handle the overall management of campaigns within the game
//Creating campaigns, adding/removing scenes, managing players etc.
//We need to do things like imposing a limit of 5 scenes per campaign for space and performance reasons

//JSON.UTILITY - We use this to persist campaign data
public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }
    
    private Campaign currentCampaign;
    private const int MAX_SCENES = 5;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Create a new campaign - need to pass in the campaign name and dm user id
    //CIAN WE NEED TO DISCUSS HOW WE'RE HANDLING USERS/DMs AND THEIR ID - need to make sure things are unique/secure
    // - FIREBASE AUTH OR SOMETHING ELSE?
    public Campaign CreateCampaign(string campaignName, string dmUserId)
    {
        currentCampaign = new Campaign(campaignName, dmUserId);
        SaveCampaign();
        Debug.Log($"Campaign created: {campaignName} with code: {currentCampaign.inviteCode}");
        return currentCampaign;
    }
    
    //Method to add a new "scene" to the current campaign
    public bool AddScene(string sceneName, SceneType sceneType, string description)
    {
        if (currentCampaign == null)
        {
            Debug.LogError("No active campaign!");
            return false;
        }
        
        if (currentCampaign.scenes.Count >= MAX_SCENES)
        {
            Debug.LogWarning("Maximum scene limit reached (5 scenes)");
            return false;
        }
        
        //Instantiate and add the new scene to the specific campaign
        SceneData newScene = new SceneData(sceneName, sceneType, description);
        currentCampaign.scenes.Add(newScene);
        //We then just save the campaign again to persist the changes
        SaveCampaign();
        Debug.Log($"Scene added: {sceneName}");
        return true;
    }
    
    //Method to remove a scene from the current campaign by its ID
    public bool RemoveScene(string sceneId)
    {
        if (currentCampaign == null)
        {
            return false;
        } 
        
        //Find and remove the scene with the given ID
        int removed = currentCampaign.scenes.RemoveAll(s => s.sceneId == sceneId);
        //If its removed successfully we save the campaign again - persist changes
        if (removed > 0)
        {
            SaveCampaign();
            Debug.Log("Scene removed");
            return true;
        }
        return false;
    }
    
    //Method to add a player to the current campaign using an invite code and their player ID
    public bool AddPlayer(string inviteCode, string playerId)
    {
        Campaign campaign = LoadCampaignByInviteCode(inviteCode);
        if (campaign != null && !campaign.playerIds.Contains(playerId))
        {
            campaign.playerIds.Add(playerId);
            currentCampaign = campaign;
            SaveCampaign();
            Debug.Log($"Player {playerId} joined campaign");
            return true;
        }
        return false;
    }
    
    public Campaign GetCurrentCampaign()
    {
        return currentCampaign;
    }
    
    public int GetSceneCount()
    {
        return currentCampaign?.scenes.Count ?? 0;
    }
    
    public bool CanAddScene()
    {
        return currentCampaign != null && currentCampaign.scenes.Count < MAX_SCENES;
    }
    
    //Basic save/load methods using PlayerPrefs and JSON utility
    private void SaveCampaign()
    {
        if (currentCampaign != null)
        {
            string json = JsonUtility.ToJson(currentCampaign);
            PlayerPrefs.SetString($"Campaign_{currentCampaign.campaignId}", json);
            PlayerPrefs.SetString("LastCampaignId", currentCampaign.campaignId);
            PlayerPrefs.Save();
        }
    }
    
    public Campaign LoadCampaign(string campaignId)
    {
        string json = PlayerPrefs.GetString($"Campaign_{campaignId}", "");
        if (!string.IsNullOrEmpty(json))
        {
            currentCampaign = JsonUtility.FromJson<Campaign>(json);
            return currentCampaign;
        }
        return null;
    }
    
    //NEED TO CHANGE THIS METHOD IN THE FUTURE - THIS IS JUST A BASIC WAY TO LOAD CAMPAIGNS BASED ON INVITE CODES
    //IN THE FUTURE WE MAY WANT TO INTEGRATE WITH A BACKEND SERVICE TO MANAGE CAMPAIGNS AND PLAYERS MORE SECURELY
    private Campaign LoadCampaignByInviteCode(string inviteCode)
    {
        string lastCampaignId = PlayerPrefs.GetString("LastCampaignId", "");
        if (!string.IsNullOrEmpty(lastCampaignId))
        {
            Campaign campaign = LoadCampaign(lastCampaignId);
            if (campaign != null && campaign.inviteCode == inviteCode)
            {
                return campaign;
            }
        }
        return null;
    }
}
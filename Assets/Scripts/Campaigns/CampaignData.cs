using System;
using System.Collections.Generic;
using UnityEngine;

//This class will handle/encapsulate all basic data relating to campaigns in general
//e.g. the campaign name, who is the dm, a list of the players, a list of scenes etc.

[System.Serializable]
public class Campaign
{
    public string campaignId;
    public string campaignName;
    public string dmUserId;
    public List<string> playerIds;
    public List<SceneData> scenes;
    public string inviteCode;
    public string createdDate;
    
    public Campaign(string name, string dmId)
    {
        campaignId = Guid.NewGuid().ToString();
        campaignName = name;
        dmUserId = dmId;
        playerIds = new List<string>();
        scenes = new List<SceneData>();
        inviteCode = GenerateInviteCode();
        createdDate = DateTime.Now.ToString();
    }
    
    //This will create a random string to serve as the invite code for players to join the campaign
    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        return new string(code);
    }
}

//This class will handle individual scene data within a campaign
//Like if its a roleplay scene or combat scene etc.
[System.Serializable]
public class SceneData
{
    public string sceneId;
    public string sceneName;
    public SceneType sceneType;
    public string description;
    
    public SceneData(string name, SceneType type, string desc)
    {
        sceneId = Guid.NewGuid().ToString();
        sceneName = name;
        sceneType = type;
        description = desc;
    }
}

public enum SceneType
{
    Roleplay,
    Combat
}
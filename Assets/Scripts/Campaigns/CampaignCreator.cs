using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

//This script will basically help with some of the behaviour for initial campaign creation
public class CampaignCreator : MonoBehaviour
{
    //Variables for campaign creation UI elements
    [SerializeField] private TMP_InputField campaignNameInput;
    [SerializeField] private TMP_InputField campaignDescriptionInput;
    [SerializeField] private Image campaignLogoImage;

    //Function to set a new image for the campaign logo
    public void SetCampaignLogo(){
        #if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Choose token image", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return;
            LoadTextureFromFile(path);
        #else
                // Runtime fallback (left simple)
        #endif
    }

    void LoadTextureFromFile(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogError("Failed to load image.");
                return;
            }
            campaignLogoImage.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(.5f,.5f));
            campaignLogoImage.preserveAspect = true;
            Debug.Log("Campaign logo loaded.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading image: " + ex.Message);
        }
    }
}

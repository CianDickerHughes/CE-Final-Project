using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Button createCampaignButton;
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private CampaignSelector campaignSelector;

    private string logoPath = "";

    void Start()
    {
        if (createCampaignButton != null)
            createCampaignButton.onClick.AddListener(OnCreateCampaignButtonClicked);
    }


        //Function to set a new image for the campaign logo
        public void SetCampaignLogo(){
    #if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Choose campaign logo", "", "png,jpg,jpeg");
        if (string.IsNullOrEmpty(path)) return;
        // Load and display the image
        LoadTextureFromFile(path);
        // Save the image to the campaigns folder
        string folder = CampaignManager.GetCampaignsFolder();
        string fileName = Path.GetFileName(path);
        string destPath = Path.Combine(folder, fileName);
        File.Copy(path, destPath, true);
        logoPath = fileName; // Store only the file name for portability
    #else
        Debug.LogWarning("Campaign logo selection is only supported in the Unity Editor.");
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

    //Called when the user clicks the Create button
    public void OnCreateCampaignButtonClicked()
    {
        //Validate inputs
        string name = campaignNameInput != null ? campaignNameInput.text.Trim() : "";
        string desc = campaignDescriptionInput != null ? campaignDescriptionInput.text.Trim() : "";
        string dm = SessionManager.Instance != null ? SessionManager.Instance.CurrentUsername : "";
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dm))
        {
            Debug.LogWarning("Campaign name or DM username is missing.");
            return;
        }

        Debug.Log($"campaignNameInput: {campaignNameInput}");
        Debug.Log($"campaignDescriptionInput: {campaignDescriptionInput}");
        Debug.Log($"SessionManager.Instance: {SessionManager.Instance}");
        Debug.Log($"CampaignManager.Instance: {CampaignManager.Instance}");
        Debug.Log($"campaignSelector: {campaignSelector}");

        //Create the campaign
        Campaign newCampaign = CampaignManager.Instance.CreateCampaign(name, dm, desc);
        
        //If we have a logo, copy it to the campaign's own folder
        if (!string.IsNullOrEmpty(logoPath))
        {
            string campaignFolder = CampaignManager.Instance.GetCurrentCampaignFolder();
            if (campaignFolder != null)
            {
                string destLogoPath = Path.Combine(campaignFolder, logoPath);
                //Copy the logo from the temp location to the campaign folder
                string tempLogoPath = Path.Combine(CampaignManager.GetCampaignsFolder(), logoPath);
                if (File.Exists(tempLogoPath))
                {
                    File.Copy(tempLogoPath, destLogoPath, true);
                    File.Delete(tempLogoPath); //Clean up the temp file
                }
            }
            newCampaign.campaignLogoPath = logoPath;
        }

        //Optionally, save the campaign data again - just to make sure
        CampaignManager.Instance.SaveCampaign();

        //Hide the popup and clear the fields
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // Refresh the campaign list
        if (campaignSelector != null)
        {
            campaignSelector.RefreshCampaignList();
        }

        //Clearing the input fields
        if (campaignNameInput != null)
        {
            campaignNameInput.text = "";
        }
        if (campaignDescriptionInput != null)
        {
            campaignDescriptionInput.text = "";
        }
        logoPath = "";
        if (campaignLogoImage != null)
        {
            campaignLogoImage.sprite = null;
        }

        Debug.Log($"Campaign '{name}' created successfully.");
        SceneManager.LoadScene("CampaignManager");
    }
}

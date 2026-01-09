using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneCreatorUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField sceneNameInput;
    [SerializeField] private TMP_Dropdown sceneTypeDropdown;
    [SerializeField] private Button createSceneButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject popupPanel;
    
    [Header("References")]
    [SerializeField] private CampaignViewUI campaignViewUI;
    
    void Start()
    {
        if (createSceneButton != null)
            createSceneButton.onClick.AddListener(OnCreateSceneButtonClicked);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
            
        SetupDropdown();
    }
    
    private void SetupDropdown()
    {
        if (sceneTypeDropdown == null) return;
        
        sceneTypeDropdown.ClearOptions();
        sceneTypeDropdown.AddOptions(new System.Collections.Generic.List<string> 
        { 
            "Roleplay", 
            "Combat", 
            "Exploration" 
        });
    }

    private void OnCreateSceneButtonClicked()
    {
        // Validate input
        string sceneName = sceneNameInput.text.Trim();
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name cannot be empty!");
            return;
        }
        
        // Get scene type from dropdown
        SceneType sceneType = (SceneType)sceneTypeDropdown.value;
        
        // Get current campaign ID
        Campaign campaign = campaignViewUI.GetCurrentCampaign();
        if (campaign == null)
        {
            Debug.LogError("No campaign loaded!");
            return;
        }
        
        // Check if SceneDataTransfer exists
        if (SceneDataTransfer.Instance == null)
        {
            Debug.LogError("SceneDataTransfer not found! Make sure it exists in your main menu scene.");
            return;
        }
        
        // Prepare the new scene data
        SceneDataTransfer.Instance.PrepareNewScene(campaign.campaignId, sceneName, sceneType);
        
        // Load the SceneMaker scene
        SceneManager.LoadScene("SceneMaker");
    }
    
    private void OnCancelClicked()
    {
        // Clear inputs and hide popup
        sceneNameInput.text = "";
        sceneTypeDropdown.value = 0;
        
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }
    
    // Call this to show the popup (from CampaignViewUI's Add Scene button)
    public void ShowPopup()
    {
        sceneNameInput.text = "";
        sceneTypeDropdown.value = 0;
        
        if (popupPanel != null)
            popupPanel.SetActive(true);
    }
}
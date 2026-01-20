using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

//This is the main controller for the CampaignManager scene
//It loads the selected campaign from CampaignSelectionContext and displays its data
//Including the campaign name, description, and list of scenes
//Basically just sets up the view/UI

public class CampaignViewUI : MonoBehaviour
{
    [Header("Campaign Info")]
    [SerializeField] private TextMeshProUGUI campaignTitleText;
    [SerializeField] private TextMeshProUGUI campaignDescriptionText;
    [SerializeField] private TextMeshProUGUI inviteCodeText;
    [SerializeField] private Image campaignLogoImage;
    
    [Header("Scenes List")]
    [SerializeField] private Transform scenesListContainer;
    [SerializeField] private GameObject sceneItemPrefab;
    [SerializeField] private TextMeshProUGUI emptyScenesText; // Optional: show when no scenes
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button addSceneButton;
    [SerializeField] private Button createSceneButton;

    [Header("Popups")]
    [SerializeField] private GameObject createScenePopup;
    
    //The currently loaded campaign
    private Campaign currentCampaign;
    private string currentFilePath;
    
    void Start()
    {
        //Set up button listeners
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
            
        if (addSceneButton != null)
            addSceneButton.onClick.AddListener(OnAddSceneClicked);
        
        //Load the campaign from context
        LoadCampaignFromContext();
    }
    
    //Load the campaign using the selection context
    private void LoadCampaignFromContext()
    {
        //Check if we have a selection
        if (!CampaignSelectionContext.HasSelection)
        {
            Debug.LogError("No campaign selected! Returning to campaign list.");
            SceneManager.LoadScene("Campaigns");
            return;
        }
        
        currentFilePath = CampaignSelectionContext.SelectedCampaignFilePath;
        
        //Load the campaign from file
        if (!File.Exists(currentFilePath))
        {
            Debug.LogError($"Campaign file not found: {currentFilePath}");
            SceneManager.LoadScene("Campaigns");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(currentFilePath);
            currentCampaign = JsonUtility.FromJson<Campaign>(json);
            
            //Also set this as the current campaign in CampaignManager
            //This allows other scripts to access it via CampaignManager.Instance.GetCurrentCampaign()
            if (CampaignManager.Instance != null)
            {
                CampaignManager.Instance.LoadCampaign(currentCampaign.campaignId);
            }
            
            //Populate the UI
            PopulateCampaignInfo();
            PopulateScenesList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load campaign: {ex.Message}");
            SceneManager.LoadScene("Campaigns");
        }
    }
    
    //Fill in the campaign info UI elements
    private void PopulateCampaignInfo()
    {
        if (currentCampaign == null) return;
        
        if (campaignTitleText != null)
            campaignTitleText.text = currentCampaign.campaignName;
            
        if (campaignDescriptionText != null)
            campaignDescriptionText.text = currentCampaign.campaignDescription;
            
        if (inviteCodeText != null)
            inviteCodeText.text = $"Invite Code: {currentCampaign.inviteCode}";
        
        //Load logo if available
        if (campaignLogoImage != null && !string.IsNullOrEmpty(currentCampaign.campaignLogoPath))
        {
            string logoPath = Path.Combine(CampaignManager.GetCampaignsFolder(), currentCampaign.campaignLogoPath);
            if (File.Exists(logoPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(logoPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    campaignLogoImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    campaignLogoImage.preserveAspect = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load campaign logo: {ex.Message}");
                }
            }
        }
    }
    
    //Populate the list of scenes for this campaign
    private void PopulateScenesList()
    {
        if (scenesListContainer == null) return;
        
        //Clear existing items
        foreach (Transform child in scenesListContainer)
        {
            Destroy(child.gameObject);
        }
        
        //Check if there are any scenes
        bool hasScenes = currentCampaign?.scenes != null && currentCampaign.scenes.Count > 0;
        
        if (emptyScenesText != null)
            emptyScenesText.gameObject.SetActive(!hasScenes);
        
        if (!hasScenes) return;
        
        //Create an item for each scene
        foreach (SceneData scene in currentCampaign.scenes)
        {
            if (sceneItemPrefab == null)
            {
                Debug.LogWarning("Scene item prefab not assigned!");
                break;
            }
            
            GameObject item = Instantiate(sceneItemPrefab, scenesListContainer);
            CampaignSceneItem sceneItem = item.GetComponent<CampaignSceneItem>();
            
            if (sceneItem != null)
            {
                //Pass the scene ID as the identifier (since scenes are stored in campaign, not as separate files)
                sceneItem.Setup(scene.sceneId, scene, OnSceneLoad, OnSceneDelete, OnSceneSettings);
            }
            else
            {
                //Fallback: just set the text if there's a TextMeshProUGUI component
                TextMeshProUGUI textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"{scene.sceneName} ({scene.sceneType})";
                }
            }
        }
    }
    
    //Called when a scene Load button is clicked
    private void OnSceneLoad(string sceneId)
    {
        SceneData scene = currentCampaign?.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            Debug.Log($"Loading scene for play: {scene.sceneName}");
            
            //Save the current scene to CurrentScene.json (for networking sync)
            SaveCurrentScene(scene);
        }
    }
    
    //Save the selected scene to CurrentScene.json and broadcast to clients
    private void SaveCurrentScene(SceneData scene)
    {
        if (scene == null) return;
        
        try
        {
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            string currentScenePath = Path.Combine(campaignsFolder, "CurrentScene.json");
            
            string json = JsonUtility.ToJson(scene, true);
            File.WriteAllText(currentScenePath, json);
            
            Debug.Log($"Current scene saved to: {currentScenePath}");

            // Broadcast to connected clients via SceneDataNetwork
            if (SceneDataNetwork.Instance != null)
            {
                SceneDataNetwork.Instance.SendSceneToClients(scene);
            }
            else
            {
                Debug.LogWarning("SceneDataNetwork instance not found; scene data not sent to clients.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save current scene: {ex.Message}");
        }
    }
    
    //Called when a scene Delete button is clicked
    private void OnSceneDelete(string sceneId)
    {
        if (currentCampaign == null) return;
        
        SceneData scene = currentCampaign.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            Debug.Log($"Scene deleted: {scene.sceneName}");
            
            //Remove the scene from the campaign
            currentCampaign.scenes.RemoveAll(s => s.sceneId == sceneId);
            
            //Save the campaign
            SaveCampaign();
            
            //Refresh the list
            PopulateScenesList();
        }
    }
    
    //Called when a scene Settings button is clicked
    private void OnSceneSettings(string sceneId)
    {
        SceneData scene = currentCampaign?.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            Debug.Log($"Opening editor for scene: {scene.sceneName}");
            
            // Prepare for editing existing scene
            if (SceneDataTransfer.Instance != null)
            {
                SceneDataTransfer.Instance.PrepareEditScene(currentCampaign.campaignId, scene);
                SceneManager.LoadScene("SceneMaker");
            }
            else
            {
                Debug.LogError("SceneDataTransfer not found!");
            }
        }
    }
    
    //Called when the Add Scene button is clicked
    private void OnAddSceneClicked()
    {
        //Open popup window to add a new scene
        if (createScenePopup != null)
        {
            createScenePopup.SetActive(true);
        }
    }
    
    //Called when the Back button is clicked
    private void OnBackClicked()
    {
        //Save any changes to the campaign - this then becomes "save and exit"
        SaveCampaign();

        //Clear the selection context
        CampaignSelectionContext.Clear();
        
        //Return to the campaign list
        SceneManager.LoadScene("Campaigns");
    }
    
    //Save the current campaign to file
    private void SaveCampaign()
    {
        if (currentCampaign == null || string.IsNullOrEmpty(currentFilePath)) return;
        
        try
        {
            string json = JsonUtility.ToJson(currentCampaign, true);
            File.WriteAllText(currentFilePath, json);
            Debug.Log("Campaign saved.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save campaign: {ex.Message}");
        }
    }
    
    //Public method to refresh the scenes list (can be called from other scripts)
    public void RefreshScenesList()
    {
        PopulateScenesList();
    }
    
    //Public getter for the current campaign
    public Campaign GetCurrentCampaign()
    {
        return currentCampaign;
    }
}

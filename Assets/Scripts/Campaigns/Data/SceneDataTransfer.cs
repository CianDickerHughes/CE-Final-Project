using UnityEngine;

/// <summary>
/// Singleton that persists between Unity scene loads.
/// Used to pass scene/map data when transitioning from Campaign Manager → Scene Maker → Gameplay.
/// 
/// SETUP: Create an empty GameObject in your first loading scene (main menu),
/// name it "SceneDataTransfer", and add this component. It will persist automatically.
/// </summary>
public class SceneDataTransfer : MonoBehaviour
{
    public static SceneDataTransfer Instance { get; private set; }

    [Header("Scene Transfer Data")]
    [SerializeField] private SceneData pendingScene;
    [SerializeField] private string currentCampaignId;
    [SerializeField] private bool isEditingExisting;

    void Awake()
    {
        //Singleton pattern - only one instance persists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneDataTransfer initialized and will persist across scenes.");
        }
        else
        {
            Debug.Log("SceneDataTransfer duplicate destroyed.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this before loading the Scene Maker to create a NEW scene.
    /// </summary>
    /// <param name="campaignId">The campaign this scene belongs to</param>
    /// <param name="sceneName">Name for the new scene</param>
    /// <param name="sceneType">Type of scene (Combat, Roleplay, Exploration)</param>
    public void PrepareNewScene(string campaignId, string sceneName, SceneType sceneType)
    {
        currentCampaignId = campaignId;
        pendingScene = new SceneData(sceneName, sceneType, "");
        isEditingExisting = false;
        Debug.Log($"Prepared new scene: {sceneName} ({sceneType}) for campaign {campaignId}");
    }

    /// <summary>
    /// Call this before loading the Scene Maker to EDIT an existing scene.
    /// </summary>
    /// <param name="campaignId">The campaign this scene belongs to</param>
    /// <param name="existingScene">The existing scene data to edit</param>
    public void PrepareEditScene(string campaignId, SceneData existingScene)
    {
        currentCampaignId = campaignId;
        pendingScene = existingScene;
        isEditingExisting = true;
        Debug.Log($"Prepared to edit scene: {existingScene.sceneName}");
    }

    /// <summary>
    /// Call this before loading a Gameplay scene to play the scene.
    /// </summary>
    /// <param name="campaignId">The campaign this scene belongs to</param>
    /// <param name="sceneToPlay">The scene data to load for gameplay</param>
    public void PreparePlayScene(string campaignId, SceneData sceneToPlay)
    {
        currentCampaignId = campaignId;
        pendingScene = sceneToPlay;
        isEditingExisting = false; // Not editing, playing
        Debug.Log($"Prepared to play scene: {sceneToPlay.sceneName}");
    }

    /// <summary>
    /// Get the pending scene data. Called by Scene Maker or Gameplay scenes on load.
    /// </summary>
    public SceneData GetPendingScene()
    {
        return pendingScene;
    }

    /// <summary>
    /// Get the current campaign ID.
    /// </summary>
    public string GetCurrentCampaignId()
    {
        return currentCampaignId;
    }

    /// <summary>
    /// Check if we're editing an existing scene vs creating new.
    /// </summary>
    public bool IsEditingExistingScene()
    {
        return isEditingExisting;
    }

    /// <summary>
    /// Update the pending scene data (called when saving in Scene Maker).
    /// </summary>
    public void UpdatePendingScene(SceneData updatedScene)
    {
        pendingScene = updatedScene;
        Debug.Log($"Scene data updated: {updatedScene.sceneName}");
    }

    /// <summary>
    /// Clear all pending data (call after successfully saving or canceling).
    /// </summary>
    public void ClearPendingData()
    {
        pendingScene = null;
        currentCampaignId = null;
        isEditingExisting = false;
        Debug.Log("SceneDataTransfer: Pending data cleared.");
    }

    /// <summary>
    /// Check if there's valid pending scene data.
    /// </summary>
    public bool HasPendingScene()
    {
        return pendingScene != null && !string.IsNullOrEmpty(currentCampaignId);
    }
}

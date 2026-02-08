using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Optional UI component for displaying Compass version control information.
/// Attach to a panel with the required UI elements to show commit history, status, etc.
/// </summary>
public class CompassUI : MonoBehaviour
{
    [Header("History Panel")]
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private Transform historyContainer;
    [SerializeField] private GameObject commitItemPrefab;
    [SerializeField] private Button showHistoryButton;
    [SerializeField] private Button closeHistoryButton;
    
    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image statusIndicator;
    [SerializeField] private Color savedColor = Color.green;
    [SerializeField] private Color unsavedColor = Color.yellow;
    
    [Header("Revert Confirmation")]
    [SerializeField] private GameObject revertConfirmPanel;
    [SerializeField] private TextMeshProUGUI revertMessageText;
    [SerializeField] private Button confirmRevertButton;
    [SerializeField] private Button cancelRevertButton;
    
    private string pendingRevertCommitId;
    private string currentSceneId;
    
    void Start()
    {
        // Set up button listeners
        if (showHistoryButton != null)
            showHistoryButton.onClick.AddListener(ShowHistory);
        
        if (closeHistoryButton != null)
            closeHistoryButton.onClick.AddListener(HideHistory);
        
        if (confirmRevertButton != null)
            confirmRevertButton.onClick.AddListener(ConfirmRevert);
        
        if (cancelRevertButton != null)
            cancelRevertButton.onClick.AddListener(CancelRevert);
        
        // Start with panels hidden
        if (historyPanel != null)
            historyPanel.SetActive(false);
        
        if (revertConfirmPanel != null)
            revertConfirmPanel.SetActive(false);
    }
    
    /// <summary>
    /// Update the status display for the current scene.
    /// </summary>
    public void UpdateStatus(SceneData currentScene)
    {
        if (currentScene == null) return;
        
        currentSceneId = currentScene.sceneId;
        
        if (CampaignManager.Instance == null) return;
        
        CompassStatus status = CampaignManager.Instance.GetSceneStatus(currentScene);
        
        if (statusText != null)
        {
            if (status.hasChanges)
            {
                statusText.text = $"Unsaved changes ({status.modifiedFields.Count} fields modified)";
            }
            else
            {
                statusText.text = $"On {status.currentBranch} - No changes";
            }
        }
        
        if (statusIndicator != null)
        {
            statusIndicator.color = status.hasChanges ? unsavedColor : savedColor;
        }
    }
    
    /// <summary>
    /// Show the commit history panel.
    /// </summary>
    public void ShowHistory()
    {
        if (historyPanel == null || string.IsNullOrEmpty(currentSceneId)) return;
        
        historyPanel.SetActive(true);
        PopulateHistory();
    }
    
    /// <summary>
    /// Hide the commit history panel.
    /// </summary>
    public void HideHistory()
    {
        if (historyPanel != null)
            historyPanel.SetActive(false);
    }
    
    /// <summary>
    /// Populate the history container with commit items.
    /// </summary>
    private void PopulateHistory()
    {
        if (historyContainer == null || commitItemPrefab == null) return;
        
        // Clear existing items
        foreach (Transform child in historyContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Get commit history
        List<CompassCommit> history = CampaignManager.Instance?.GetSceneHistory(currentSceneId, 10);
        
        if (history == null || history.Count == 0)
        {
            // Show "No history" message
            CreateHistoryMessage("No commit history available");
            return;
        }
        
        // Create an item for each commit
        foreach (CompassCommit commit in history)
        {
            CreateCommitItem(commit);
        }
    }
    
    /// <summary>
    /// Create a UI item for a commit.
    /// </summary>
    private void CreateCommitItem(CompassCommit commit)
    {
        GameObject itemObj = Instantiate(commitItemPrefab, historyContainer);
        
        // Set commit info
        TextMeshProUGUI messageText = itemObj.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
            messageText.text = commit.message;
        
        TextMeshProUGUI detailsText = itemObj.transform.Find("DetailsText")?.GetComponent<TextMeshProUGUI>();
        if (detailsText != null)
            detailsText.text = $"{commit.commitId} | {commit.author} | {commit.timestamp}";
        
        // Set up revert button
        Button revertButton = itemObj.transform.Find("RevertButton")?.GetComponent<Button>();
        if (revertButton != null)
        {
            string commitId = commit.commitId;
            string message = commit.message;
            revertButton.onClick.AddListener(() => ShowRevertConfirmation(commitId, message));
        }
    }
    
    /// <summary>
    /// Create a simple message in the history container.
    /// </summary>
    private void CreateHistoryMessage(string message)
    {
        GameObject msgObj = new GameObject("HistoryMessage");
        msgObj.transform.SetParent(historyContainer, false);
        
        TextMeshProUGUI text = msgObj.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;
    }
    
    /// <summary>
    /// Show the revert confirmation dialog.
    /// </summary>
    private void ShowRevertConfirmation(string commitId, string commitMessage)
    {
        pendingRevertCommitId = commitId;
        
        if (revertConfirmPanel != null)
            revertConfirmPanel.SetActive(true);
        
        if (revertMessageText != null)
            revertMessageText.text = $"Revert to commit?\n\n{commitId}\n\"{commitMessage}\"";
    }
    
    /// <summary>
    /// Confirm and execute the revert.
    /// </summary>
    private void ConfirmRevert()
    {
        if (string.IsNullOrEmpty(pendingRevertCommitId) || string.IsNullOrEmpty(currentSceneId))
        {
            CancelRevert();
            return;
        }
        
        SceneData revertedScene = CampaignManager.Instance?.RevertSceneToCommit(currentSceneId, pendingRevertCommitId);
        
        if (revertedScene != null)
        {
            Debug.Log($"Compass UI: Reverted to commit {pendingRevertCommitId}");
            
            // Refresh the history
            PopulateHistory();
            
            // Update SceneDataTransfer so the scene reloads with reverted data
            if (SceneDataTransfer.Instance != null)
            {
                SceneDataTransfer.Instance.UpdatePendingScene(revertedScene);
            }
        }
        
        CancelRevert();
    }
    
    /// <summary>
    /// Cancel the revert operation.
    /// </summary>
    private void CancelRevert()
    {
        pendingRevertCommitId = null;
        
        if (revertConfirmPanel != null)
            revertConfirmPanel.SetActive(false);
    }
    
    /// <summary>
    /// Get a formatted summary of the Compass status for display.
    /// </summary>
    public string GetStatusSummary()
    {
        if (string.IsNullOrEmpty(currentSceneId))
            return "No scene loaded";
        
        return CampaignManager.Instance?.GetSceneCompassSummary(currentSceneId) ?? "Compass unavailable";
    }
}

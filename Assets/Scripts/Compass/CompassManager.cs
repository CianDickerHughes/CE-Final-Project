using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CompassManager - The main controller for the Git-like version control system.
/// Handles commit, checkout, history, diff, and other Git-like operations for campaign scenes.
/// 
/// Git Equivalent Mapping:
/// - CurrentScene.json = Working Directory
/// - Campaigns/{CampaignName}/{id}.json = Remote Main Branch
/// - .compass folder = .git folder (stores history and branches)
/// </summary>
public class CompassManager : MonoBehaviour
{
    private static CompassManager _instance;
    
    /// <summary>
    /// Gets the CompassManager instance, creating it if necessary.
    /// </summary>
    public static CompassManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindObjectOfType<CompassManager>();
                
                // If not found, create one
                if (_instance == null)
                {
                    GameObject compassObj = new GameObject("CompassManager");
                    _instance = compassObj.AddComponent<CompassManager>();
                    Debug.Log("CompassManager: Auto-created instance.");
                }
            }
            return _instance;
        }
    }
    
    // Cache of loaded repositories
    private Dictionary<string, CompassRepository> repositoryCache;
    
    // Constants
    private const string COMPASS_FOLDER = ".compass";
    private const string MAIN_BRANCH = "main";
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            repositoryCache = new Dictionary<string, CompassRepository>();
            Debug.Log("CompassManager initialized.");
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Ensure the repository cache is initialized.
    /// </summary>
    private void EnsureCacheInitialized()
    {
        if (repositoryCache == null)
        {
            repositoryCache = new Dictionary<string, CompassRepository>();
        }
    }
    
    #region Core Git-like Operations
    
    /// <summary>
    /// Initialize a new Compass repository for a scene.
    /// Equivalent to: git init
    /// </summary>
    public CompassRepository Init(string campaignName, SceneData scene)
    {
        string repoPath = GetRepositoryPath(campaignName, scene.sceneId);
        
        // Check if repo already exists
        if (File.Exists(repoPath))
        {
            Debug.Log($"Compass: Repository already exists for scene {scene.sceneName}");
            return LoadRepository(campaignName, scene.sceneId);
        }
        
        // Create new repository
        CompassRepository repo = new CompassRepository(scene.sceneId, scene.sceneName);
        repo.workingSceneJson = JsonUtility.ToJson(scene);
        
        // Create initial commit
        CompassCommit initialCommit = new CompassCommit(
            "Initial scene creation",
            GetCurrentUser(),
            scene
        );
        
        repo.commits.Add(initialCommit);
        repo.GetBranch(MAIN_BRANCH).headCommitId = initialCommit.commitId;
        
        // Save repository
        EnsureCacheInitialized();
        SaveRepository(campaignName, repo);
        repositoryCache[scene.sceneId] = repo;
        
        Debug.Log($"Compass: Initialized repository for scene {scene.sceneName} with initial commit {initialCommit.commitId}");
        return repo;
    }
    
    /// <summary>
    /// Commit the current working state to the repository.
    /// Equivalent to: git commit -m "message"
    /// This also updates the main campaign file (like pushing to main).
    /// </summary>
    public CompassCommit Commit(string campaignName, SceneData currentScene, string message)
    {
        CompassRepository repo = GetOrCreateRepository(campaignName, currentScene);
        
        // Get current HEAD
        string parentId = repo.GetBranch(repo.currentBranch)?.headCommitId ?? "";
        
        // Create new commit
        CompassCommit newCommit = new CompassCommit(
            message,
            GetCurrentUser(),
            currentScene,
            parentId
        );
        
        repo.commits.Add(newCommit);
        
        // Update branch HEAD
        var branch = repo.GetBranch(repo.currentBranch);
        if (branch != null)
        {
            branch.headCommitId = newCommit.commitId;
        }
        
        // Update working state
        repo.workingSceneJson = JsonUtility.ToJson(currentScene);
        
        // Save repository
        SaveRepository(campaignName, repo);
        
        // Also push to main campaign file if on main branch
        if (repo.currentBranch == MAIN_BRANCH)
        {
            PushToMain(campaignName, currentScene);
        }
        
        Debug.Log($"Compass: Committed {newCommit.commitId} - \"{message}\"");
        return newCommit;
    }
    
    /// <summary>
    /// Checkout a specific commit and restore that state.
    /// Equivalent to: git checkout <commit-id>
    /// </summary>
    public SceneData Checkout(string campaignName, string sceneId, string commitId)
    {
        CompassRepository repo = LoadRepository(campaignName, sceneId);
        if (repo == null)
        {
            Debug.LogError($"Compass: Repository not found for scene {sceneId}");
            return null;
        }
        
        CompassCommit commit = repo.GetCommit(commitId);
        if (commit == null)
        {
            Debug.LogError($"Compass: Commit {commitId} not found");
            return null;
        }
        
        SceneData sceneData = commit.GetSceneData();
        if (sceneData != null)
        {
            repo.workingSceneJson = JsonUtility.ToJson(sceneData);
            SaveRepository(campaignName, repo);
            Debug.Log($"Compass: Checked out commit {commitId}");
        }
        
        return sceneData;
    }
    
    /// <summary>
    /// Get the commit history for a scene.
    /// Equivalent to: git log
    /// </summary>
    public List<CompassCommit> Log(string campaignName, string sceneId, int maxCount = 10)
    {
        CompassRepository repo = LoadRepository(campaignName, sceneId);
        if (repo == null)
        {
            Debug.LogWarning($"Compass: No repository found for scene {sceneId}");
            return new List<CompassCommit>();
        }
        
        return repo.GetHistory(maxCount);
    }
    
    /// <summary>
    /// Check the status of the working directory vs HEAD.
    /// Equivalent to: git status
    /// </summary>
    public CompassStatus Status(string campaignName, SceneData currentScene)
    {
        CompassStatus status = new CompassStatus();
        
        CompassRepository repo = LoadRepository(campaignName, currentScene.sceneId);
        if (repo == null)
        {
            status.hasChanges = true;
            status.modifiedFields.Add("New scene (not yet tracked by Compass)");
            return status;
        }
        
        status.currentBranch = repo.currentBranch;
        var headCommit = repo.GetHeadCommit();
        status.headCommitId = headCommit?.commitId ?? "";
        
        // Compare current scene with HEAD
        if (headCommit != null)
        {
            SceneData headScene = headCommit.GetSceneData();
            status.modifiedFields = CompareScenes(headScene, currentScene);
            status.hasChanges = status.modifiedFields.Count > 0;
        }
        else
        {
            status.hasChanges = true;
            status.modifiedFields.Add("No commits yet");
        }
        
        return status;
    }
    
    /// <summary>
    /// Get the difference between two commits.
    /// Equivalent to: git diff <commit1> <commit2>
    /// </summary>
    public CompassDiff Diff(string campaignName, string sceneId, string fromCommitId, string toCommitId)
    {
        CompassRepository repo = LoadRepository(campaignName, sceneId);
        if (repo == null)
            return null;
        
        CompassCommit fromCommit = repo.GetCommit(fromCommitId);
        CompassCommit toCommit = repo.GetCommit(toCommitId);
        
        if (fromCommit == null || toCommit == null)
        {
            Debug.LogError("Compass: One or both commits not found");
            return null;
        }
        
        CompassDiff diff = new CompassDiff(fromCommitId, toCommitId);
        
        SceneData fromScene = fromCommit.GetSceneData();
        SceneData toScene = toCommit.GetSceneData();
        
        diff.changes = CompareScenes(fromScene, toScene);
        
        return diff;
    }
    
    /// <summary>
    /// Revert to a previous commit's state.
    /// Equivalent to: git revert or git reset
    /// </summary>
    public SceneData Revert(string campaignName, string sceneId, string commitId, string revertMessage = "")
    {
        CompassRepository repo = LoadRepository(campaignName, sceneId);
        if (repo == null)
        {
            Debug.LogError($"Compass: Repository not found for scene {sceneId}");
            return null;
        }
        
        CompassCommit targetCommit = repo.GetCommit(commitId);
        if (targetCommit == null)
        {
            Debug.LogError($"Compass: Commit {commitId} not found");
            return null;
        }
        
        SceneData revertedScene = targetCommit.GetSceneData();
        if (revertedScene == null)
        {
            Debug.LogError("Compass: Could not deserialize scene from commit");
            return null;
        }
        
        // Create a new commit with the reverted state
        string message = string.IsNullOrEmpty(revertMessage) 
            ? $"Revert to commit {commitId}" 
            : revertMessage;
        
        Commit(campaignName, revertedScene, message);
        
        Debug.Log($"Compass: Reverted to commit {commitId}");
        return revertedScene;
    }
    
    #endregion
    
    #region Save and Load Operations
    
    /// <summary>
    /// Quick save - commits and updates the main campaign file.
    /// This is called when the Host/DM clicks "Save and Exit".
    /// </summary>
    public CompassCommit SaveAndCommit(string campaignName, SceneData sceneData, string message = "")
    {
        if (string.IsNullOrEmpty(message))
        {
            message = $"Auto-save at {DateTime.Now:HH:mm:ss}";
        }
        
        // Commit the changes
        CompassCommit commit = Commit(campaignName, sceneData, message);
        
        // Update CurrentScene.json
        SaveToCurrentSceneFile(sceneData);
        
        // Push to main campaign file
        PushToMain(campaignName, sceneData);
        
        Debug.Log($"Compass: Save and commit complete - {commit.commitId}");
        return commit;
    }
    
    /// <summary>
    /// Push scene data to the main campaign file.
    /// Equivalent to: git push origin main
    /// </summary>
    private void PushToMain(string campaignName, SceneData sceneData)
    {
        // Update the scene in CampaignManager
        if (CampaignManager.Instance != null)
        {
            bool updated = CampaignManager.Instance.UpdateScene(sceneData);
            if (updated)
            {
                Debug.Log($"Compass: Pushed to main - scene {sceneData.sceneName} updated in campaign");
            }
            else
            {
                Debug.LogWarning($"Compass: Failed to push to main - scene not found in campaign");
            }
        }
    }
    
    /// <summary>
    /// Save scene data to CurrentScene.json (working directory).
    /// </summary>
    private void SaveToCurrentSceneFile(SceneData sceneData)
    {
        try
        {
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            string currentScenePath = Path.Combine(campaignsFolder, "CurrentScene.json");
            
            string json = JsonUtility.ToJson(sceneData, true);
            File.WriteAllText(currentScenePath, json);
            
            Debug.Log($"Compass: Working directory saved to {currentScenePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Compass: Failed to save working directory: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Pull the latest scene data from the main campaign file.
    /// Equivalent to: git pull origin main
    /// </summary>
    public SceneData Pull(string campaignName, string sceneId)
    {
        if (CampaignManager.Instance == null)
        {
            Debug.LogError("Compass: CampaignManager not available");
            return null;
        }
        
        Campaign campaign = CampaignManager.Instance.GetCurrentCampaign();
        if (campaign == null)
        {
            Debug.LogError("Compass: No active campaign");
            return null;
        }
        
        SceneData scene = campaign.scenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            Debug.Log($"Compass: Pulled latest for scene {scene.sceneName}");
        }
        
        return scene;
    }
    
    #endregion
    
    #region Repository Persistence
    
    /// <summary>
    /// Get the file path for a scene's Compass repository.
    /// </summary>
    private string GetRepositoryPath(string campaignName, string sceneId)
    {
        string campaignFolder = Path.Combine(CampaignManager.GetCampaignsFolder(), campaignName);
        string compassFolder = Path.Combine(campaignFolder, COMPASS_FOLDER);
        
        if (!Directory.Exists(compassFolder))
        {
            Directory.CreateDirectory(compassFolder);
        }
        
        return Path.Combine(compassFolder, $"{sceneId}.compass.json");
    }
    
    /// <summary>
    /// Save a repository to disk.
    /// </summary>
    private void SaveRepository(string campaignName, CompassRepository repo)
    {
        EnsureCacheInitialized();
        try
        {
            string repoPath = GetRepositoryPath(campaignName, repo.sceneId);
            string json = JsonUtility.ToJson(repo, true);
            File.WriteAllText(repoPath, json);
            
            // Update cache
            repositoryCache[repo.sceneId] = repo;
            
            Debug.Log($"Compass: Repository saved to {repoPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Compass: Failed to save repository: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load a repository from disk.
    /// </summary>
    private CompassRepository LoadRepository(string campaignName, string sceneId)
    {
        EnsureCacheInitialized();
        // Check cache first
        if (repositoryCache.ContainsKey(sceneId))
        {
            return repositoryCache[sceneId];
        }
        
        string repoPath = GetRepositoryPath(campaignName, sceneId);
        
        if (!File.Exists(repoPath))
        {
            return null;
        }
        
        try
        {
            string json = File.ReadAllText(repoPath);
            CompassRepository repo = JsonUtility.FromJson<CompassRepository>(json);
            repositoryCache[sceneId] = repo;
            return repo;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Compass: Failed to load repository: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get or create a repository for a scene.
    /// </summary>
    private CompassRepository GetOrCreateRepository(string campaignName, SceneData scene)
    {
        CompassRepository repo = LoadRepository(campaignName, scene.sceneId);
        
        if (repo == null)
        {
            repo = Init(campaignName, scene);
        }
        
        return repo;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Get the current user's name for commit authorship.
    /// </summary>
    private string GetCurrentUser()
    {
        // Try to get from SessionManager
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.CurrentUsername))
        {
            return SessionManager.Instance.CurrentUsername;
        }
        
        return "Unknown";
    }
    
    /// <summary>
    /// Compare two scenes and return a list of differences.
    /// </summary>
    private List<string> CompareScenes(SceneData scene1, SceneData scene2)
    {
        List<string> changes = new List<string>();
        
        if (scene1 == null || scene2 == null)
        {
            changes.Add("One or both scenes are null");
            return changes;
        }
        
        // Compare basic properties
        if (scene1.sceneName != scene2.sceneName)
            changes.Add($"Scene name: '{scene1.sceneName}' → '{scene2.sceneName}'");
        
        if (scene1.sceneType != scene2.sceneType)
            changes.Add($"Scene type: {scene1.sceneType} → {scene2.sceneType}");
        
        if (scene1.description != scene2.description)
            changes.Add("Description modified");
        
        if (scene1.status != scene2.status)
            changes.Add($"Status: '{scene1.status}' → '{scene2.status}'");
        
        // Compare map data
        if (scene1.mapData != null && scene2.mapData != null)
        {
            if (scene1.mapData.width != scene2.mapData.width || scene1.mapData.height != scene2.mapData.height)
                changes.Add($"Map size: {scene1.mapData.width}x{scene1.mapData.height} → {scene2.mapData.width}x{scene2.mapData.height}");
            
            if (!AreTileArraysEqual(scene1.mapData.tiles, scene2.mapData.tiles))
                changes.Add("Map tiles modified");
        }
        else if (scene1.mapData == null != (scene2.mapData == null))
        {
            changes.Add("Map data added or removed");
        }
        
        // Compare tokens
        int tokens1 = scene1.tokens?.Count ?? 0;
        int tokens2 = scene2.tokens?.Count ?? 0;
        if (tokens1 != tokens2)
            changes.Add($"Token count: {tokens1} → {tokens2}");
        else if (tokens1 > 0 && !AreTokenListsEqual(scene1.tokens, scene2.tokens))
            changes.Add("Token positions modified");
        
        // Compare active characters
        int chars1 = scene1.activeCharacterIds?.Count ?? 0;
        int chars2 = scene2.activeCharacterIds?.Count ?? 0;
        if (chars1 != chars2)
            changes.Add($"Active characters: {chars1} → {chars2}");
        
        return changes;
    }
    
    /// <summary>
    /// Compare two tile arrays for equality.
    /// </summary>
    private bool AreTileArraysEqual(int[] tiles1, int[] tiles2)
    {
        if (tiles1 == null && tiles2 == null) return true;
        if (tiles1 == null || tiles2 == null) return false;
        if (tiles1.Length != tiles2.Length) return false;
        
        for (int i = 0; i < tiles1.Length; i++)
        {
            if (tiles1[i] != tiles2[i]) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Compare two token lists for equality.
    /// </summary>
    private bool AreTokenListsEqual(List<TokenData> tokens1, List<TokenData> tokens2)
    {
        if (tokens1 == null && tokens2 == null) return true;
        if (tokens1 == null || tokens2 == null) return false;
        if (tokens1.Count != tokens2.Count) return false;
        
        for (int i = 0; i < tokens1.Count; i++)
        {
            var t1 = tokens1[i];
            var t2 = tokens2[i];
            
            if (t1.characterId != t2.characterId ||
                t1.enemyId != t2.enemyId ||
                t1.tokenType != t2.tokenType ||
                t1.gridX != t2.gridX ||
                t1.gridY != t2.gridY)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Clear the repository cache.
    /// </summary>
    public void ClearCache()
    {
        EnsureCacheInitialized();
        repositoryCache.Clear();
        Debug.Log("Compass: Cache cleared");
    }
    
    /// <summary>
    /// Check if a scene has a Compass repository.
    /// </summary>
    public bool HasRepository(string campaignName, string sceneId)
    {
        string repoPath = GetRepositoryPath(campaignName, sceneId);
        return File.Exists(repoPath);
    }
    
    /// <summary>
    /// Get summary info about a scene's repository.
    /// </summary>
    public string GetRepositorySummary(string campaignName, string sceneId)
    {
        CompassRepository repo = LoadRepository(campaignName, sceneId);
        if (repo == null)
            return "No Compass history";
        
        int commitCount = repo.commits.Count;
        var headCommit = repo.GetHeadCommit();
        string lastUpdate = headCommit?.timestamp ?? "Unknown";
        
        return $"{commitCount} commits, last: {lastUpdate}";
    }
    
    #endregion
}

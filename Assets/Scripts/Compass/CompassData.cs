using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compass - A Git-like version control system for campaign scene management.
/// This file contains the data structures used by Compass to track changes.
/// </summary>
/// 
/// <summary>
/// Represents a single commit in the Compass history.
/// Similar to a Git commit, it stores a snapshot of the scene at a point in time.
/// </summary>
[System.Serializable]
public class CompassCommit
{
    public string commitId;
    public string parentCommitId;  // Reference to parent commit (for history)
    public string message;
    public string author;
    public string timestamp;
    public string sceneDataJson;   // Serialized snapshot of SceneData at commit time
    
    public CompassCommit(string message, string author, SceneData sceneData, string parentId = "")
    {
        commitId = GenerateCommitId();
        parentCommitId = parentId;
        this.message = message;
        this.author = author;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        sceneDataJson = JsonUtility.ToJson(sceneData);
    }
    
    /// <summary>
    /// Generates a short commit ID similar to Git's abbreviated hash.
    /// </summary>
    private string GenerateCommitId()
    {
        return Guid.NewGuid().ToString().Substring(0, 8);
    }
    
    /// <summary>
    /// Deserializes and returns the SceneData stored in this commit.
    /// </summary>
    public SceneData GetSceneData()
    {
        if (string.IsNullOrEmpty(sceneDataJson))
            return null;
        return JsonUtility.FromJson<SceneData>(sceneDataJson);
    }
}

/// <summary>
/// Represents a branch in the Compass system.
/// Main branch stores the canonical version, working branch stores current edits.
/// </summary>
[System.Serializable]
public class CompassBranch
{
    public string branchName;
    public string headCommitId;    // Points to the latest commit on this branch
    public string sceneId;         // The scene this branch tracks
    
    public CompassBranch(string name, string sceneId, string headCommit = "")
    {
        branchName = name;
        this.sceneId = sceneId;
        headCommitId = headCommit;
    }
}

/// <summary>
/// Represents the complete Compass repository for a single scene.
/// Contains all commits, branches, and tracking information.
/// </summary>
[System.Serializable]
public class CompassRepository
{
    public string sceneId;
    public string sceneName;
    public List<CompassCommit> commits;
    public List<CompassBranch> branches;
    public string currentBranch;   // Name of the currently active branch
    public string workingSceneJson; // Current working state (like Git's working directory)
    
    public CompassRepository(string sceneId, string sceneName)
    {
        this.sceneId = sceneId;
        this.sceneName = sceneName;
        commits = new List<CompassCommit>();
        branches = new List<CompassBranch>();
        currentBranch = "main";
        workingSceneJson = "";
        
        // Initialize with main branch
        branches.Add(new CompassBranch("main", sceneId));
    }
    
    /// <summary>
    /// Get a branch by name.
    /// </summary>
    public CompassBranch GetBranch(string branchName)
    {
        return branches.Find(b => b.branchName == branchName);
    }
    
    /// <summary>
    /// Get a commit by ID.
    /// </summary>
    public CompassCommit GetCommit(string commitId)
    {
        return commits.Find(c => c.commitId == commitId);
    }
    
    /// <summary>
    /// Get the HEAD commit (latest commit on current branch).
    /// </summary>
    public CompassCommit GetHeadCommit()
    {
        var branch = GetBranch(currentBranch);
        if (branch == null || string.IsNullOrEmpty(branch.headCommitId))
            return null;
        return GetCommit(branch.headCommitId);
    }
    
    /// <summary>
    /// Get the commit history for the current branch.
    /// Returns commits ordered from newest to oldest.
    /// </summary>
    public List<CompassCommit> GetHistory(int maxCount = 10)
    {
        List<CompassCommit> history = new List<CompassCommit>();
        var headCommit = GetHeadCommit();
        
        if (headCommit == null)
            return history;
        
        CompassCommit current = headCommit;
        while (current != null && history.Count < maxCount)
        {
            history.Add(current);
            if (string.IsNullOrEmpty(current.parentCommitId))
                break;
            current = GetCommit(current.parentCommitId);
        }
        
        return history;
    }
}

/// <summary>
/// Represents a difference between two scene states.
/// Used for showing what changed between commits.
/// </summary>
[System.Serializable]
public class CompassDiff
{
    public string fromCommitId;
    public string toCommitId;
    public List<string> changes;
    
    public CompassDiff(string from, string to)
    {
        fromCommitId = from;
        toCommitId = to;
        changes = new List<string>();
    }
    
    public void AddChange(string change)
    {
        changes.Add(change);
    }
}

/// <summary>
/// Status result for checking working directory changes.
/// </summary>
[System.Serializable]
public class CompassStatus
{
    public bool hasChanges;
    public string currentBranch;
    public string headCommitId;
    public List<string> modifiedFields;
    
    public CompassStatus()
    {
        hasChanges = false;
        modifiedFields = new List<string>();
    }
}

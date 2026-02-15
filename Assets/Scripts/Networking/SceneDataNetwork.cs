using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles networked scene data transfer from host (DM) to clients (PCs).
/// When the DM selects a scene, it gets sent to all connected players.
/// Spawn this as a network prefab similar to ChatNetwork and CharacterTransferNetwork.
/// </summary>
public class SceneDataNetwork : NetworkBehaviour
{
    public static SceneDataNetwork Instance { get; private set; }

    // Event raised when scene data is received
    public event Action<SceneData> OnSceneDataReceived;
    
    // Store campaign name for token loading on clients
    public string LastReceivedCampaignName { get; private set; }

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

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Host/DM sends the current scene data to all clients.
    /// Call this after saving CurrentScene.json.
    /// </summary>
    public void SendSceneToClients(SceneData sceneData)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("SceneDataNetwork: Not spawned yet; cannot send scene data.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("SceneDataNetwork: Only the host can send scene data.");
            return;
        }

        if (sceneData == null)
        {
            Debug.LogWarning("SceneDataNetwork: Scene data is null, cannot send.");
            return;
        }

        // Get campaign name to send to clients
        string campaignName = CampaignManager.Instance?.GetCurrentCampaign()?.campaignName ?? "";

        // Serialize the scene data to JSON
        string jsonContent = JsonUtility.ToJson(sceneData, true);
        
        // FIRST send token images so they arrive before the scene data
        SendTokenImages(sceneData, campaignName);
        
        // Then send scene data to all clients
        SendSceneDataClientRpc(jsonContent, campaignName);
        
        Debug.Log($"SceneDataNetwork: Broadcasting scene '{sceneData.sceneName}' with campaign '{campaignName}' to all clients.");
    }

    /// <summary>
    /// Send all token images for the scene to clients
    /// </summary>
    private void SendTokenImages(SceneData sceneData, string campaignName)
    {
        if (sceneData?.tokens == null || sceneData.tokens.Count == 0)
        {
            Debug.Log("SceneDataNetwork: No tokens to send images for");
            return;
        }

        Debug.Log($"SceneDataNetwork: Sending images for {sceneData.tokens.Count} tokens");

        HashSet<string> sentImages = new HashSet<string>();

        foreach (TokenData token in sceneData.tokens)
        {
            if (string.IsNullOrEmpty(token.tokenFileName) || sentImages.Contains(token.tokenFileName))
                continue;

            try
            {
                // Find the image file - check multiple locations
                string imagePath = FindTokenImagePath(token.tokenFileName, campaignName);
                
                if (string.IsNullOrEmpty(imagePath))
                {
                    Debug.LogWarning($"SceneDataNetwork: Cannot find image '{token.tokenFileName}' in any location");
                    continue;
                }

                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string imageBase64 = Convert.ToBase64String(imageBytes);
                
                Debug.Log($"SceneDataNetwork: Sending image '{token.tokenFileName}' ({imageBytes.Length} bytes) from {imagePath}");

                // Send to all clients
                ReceiveTokenImageClientRpc(token.tokenFileName, imageBase64, campaignName);
                sentImages.Add(token.tokenFileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneDataNetwork: Failed to send image '{token.tokenFileName}': {ex.Message}");
            }
        }

        Debug.Log($"SceneDataNetwork: Sent {sentImages.Count} token images");
    }

    /// <summary>
    /// Find a token image file by searching multiple locations
    /// </summary>
    private string FindTokenImagePath(string tokenFileName, string campaignName)
    {
        // Location 1: Main Characters folder
        string mainFolder = CharacterIO.GetCharactersFolder();
        string path1 = Path.Combine(mainFolder, tokenFileName);
        if (File.Exists(path1))
        {
            Debug.Log($"SceneDataNetwork: Found image in main folder: {path1}");
            return path1;
        }

        // Location 2: Campaign Characters folder
        if (!string.IsNullOrEmpty(campaignName))
        {
            #if UNITY_EDITOR
                string campaignPath = Path.Combine(Application.dataPath, "Campaigns", campaignName, "Characters");
            #else
                string campaignPath = Path.Combine(Application.persistentDataPath, "Campaigns", campaignName, "Characters");
            #endif

            string path2 = Path.Combine(campaignPath, tokenFileName);
            if (File.Exists(path2))
            {
                Debug.Log($"SceneDataNetwork: Found image in campaign folder: {path2}");
                return path2;
            }

            // Location 3: Campaign PlayerCharacters folder
            #if UNITY_EDITOR
                string playerCharsPath = Path.Combine(Application.dataPath, "Campaigns", campaignName, "PlayerCharacters");
            #else
                string playerCharsPath = Path.Combine(Application.persistentDataPath, "Campaigns", campaignName, "PlayerCharacters");
            #endif

            string path3 = Path.Combine(playerCharsPath, tokenFileName);
            if (File.Exists(path3))
            {
                Debug.Log($"SceneDataNetwork: Found image in PlayerCharacters folder: {path3}");
                return path3;
            }
        }

        Debug.LogWarning($"SceneDataNetwork: Image '{tokenFileName}' not found. Checked: {path1}");
        return null;
    }

    /// <summary>
    /// Client receives a token image and saves it locally
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveTokenImageClientRpc(string fileName, string imageBase64, string campaignName)
    {
        try
        {
            Debug.Log($"SceneDataNetwork: Receiving image '{fileName}' for campaign '{campaignName}'");

            if (string.IsNullOrEmpty(campaignName))
            {
                Debug.LogError("SceneDataNetwork: Cannot save image - campaign name is empty");
                return;
            }

            // Save to ReceivedCampaigns folder
            string savePath;
            #if UNITY_EDITOR
                savePath = Path.Combine(Application.dataPath, "Campaigns", "ReceivedCampaigns", campaignName, "Characters");
            #else
                savePath = Path.Combine(Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaignName, "Characters");
            #endif

            // Create directory
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Debug.Log($"SceneDataNetwork: Created folder: {savePath}");
            }

            // Decode and save
            byte[] imageBytes = Convert.FromBase64String(imageBase64);
            string fullPath = Path.Combine(savePath, fileName);
            File.WriteAllBytes(fullPath, imageBytes);

            Debug.Log($"SceneDataNetwork: Saved image to: {fullPath} ({imageBytes.Length} bytes)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneDataNetwork: Failed to save image '{fileName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Receives scene data on all clients and saves it locally.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void SendSceneDataClientRpc(string sceneJson, string campaignName)
    {
        try
        {
            // Store campaign name for token loading
            LastReceivedCampaignName = campaignName;
            Debug.Log($"SceneDataNetwork: Received campaign name: {campaignName}");

            // Deserialize the scene data
            SceneData sceneData = JsonUtility.FromJson<SceneData>(sceneJson);
            
            if (sceneData != null)
            {
                // Save to local CurrentScene.json
                SaveCurrentSceneLocally(sceneData, sceneJson);
                
                // Notify listeners
                OnSceneDataReceived?.Invoke(sceneData);
                
                Debug.Log($"SceneDataNetwork: Received scene data: {sceneData.sceneName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneDataNetwork: Failed to process scene data: {ex.Message}");
        }
    }

    /// <summary>
    /// Save the received scene data to the local CurrentScene.json file.
    /// </summary>
    private void SaveCurrentSceneLocally(SceneData sceneData, string sceneJson)
    {
        try
        {
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            string currentScenePath = Path.Combine(campaignsFolder, "CurrentScene.json");
            
            // Ensure the directory exists
            if (!Directory.Exists(campaignsFolder))
            {
                Directory.CreateDirectory(campaignsFolder);
            }
            
            // Write the JSON to file
            File.WriteAllText(currentScenePath, sceneJson);
            
            Debug.Log($"SceneDataNetwork: Saved CurrentScene.json locally: {sceneData.sceneName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneDataNetwork: Failed to save scene locally: {ex.Message}");
        }
    }

    /// <summary>
    /// Load the current scene from CurrentScene.json if it exists.
    /// </summary>
    public SceneData LoadCurrentScene()
    {
        try
        {
            string campaignsFolder = CampaignManager.GetCampaignsFolder();
            string currentScenePath = Path.Combine(campaignsFolder, "CurrentScene.json");
            
            if (File.Exists(currentScenePath))
            {
                string json = File.ReadAllText(currentScenePath);
                SceneData sceneData = JsonUtility.FromJson<SceneData>(json);
                return sceneData;
            }
            else
            {
                Debug.LogWarning("SceneDataNetwork: CurrentScene.json does not exist.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SceneDataNetwork: Failed to load current scene: {ex.Message}");
            return null;
        }
    }
}

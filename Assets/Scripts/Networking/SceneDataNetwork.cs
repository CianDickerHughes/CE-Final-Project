using System;
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

        // Serialize the scene data to JSON
        string jsonContent = JsonUtility.ToJson(sceneData, true);
        
        // Send to all clients
        SendSceneDataClientRpc(jsonContent);
        
        Debug.Log($"SceneDataNetwork: Broadcasting scene '{sceneData.sceneName}' to all clients.");
    }

    /// <summary>
    /// Receives scene data on all clients and saves it locally.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void SendSceneDataClientRpc(string sceneJson)
    {
        try
        {
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

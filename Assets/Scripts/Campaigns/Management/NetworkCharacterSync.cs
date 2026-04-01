using Unity.Netcode;
using UnityEngine;

//This script handles syncing character data between clients and the host
//Players send their character to the host, and the host adds it to the campaign
//NOTE - WONT WORK PROPERLY UNTIL NETCODE IS FULLY IMPLEMENTED

public class NetworkCharacterSync : NetworkBehaviour
{
    public static NetworkCharacterSync Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    //CLIENT: Player sends their character data to the host
    public void SendCharacterToHost(string username, CharacterData character)
    {
        if (!IsClient) return;
        
        string characterJson = JsonUtility.ToJson(character);
        
        //Send to server/host
        SendCharacterDataServerRpc(username, characterJson);
    }
    
    //SERVER/HOST: Receives character data from a client
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendCharacterDataServerRpc(string username, string characterJson)
    {
        //Get the network ID of the client who sent this
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        
        //Deserialize the character
        CharacterData character = JsonUtility.FromJson<CharacterData>(characterJson);
        
        //Add to the campaign (this only happens on the host/DM's device)
        bool success = CampaignManager.Instance.AddPlayerCharacterToCampaign(username, clientId, character);
        
        if (success)
        {
            Debug.Log($"Host: Player {username} joined with character {character.charName} (NetworkId: {clientId})");
            
            //Optionally notify all clients that a player joined
            NotifyPlayerJoinedClientRpc(username, character.charName);
        }
    }
    
    //Notify all clients that a new player joined
    [ClientRpc]
    private void NotifyPlayerJoinedClientRpc(string username, string characterName)
    {
        Debug.Log($"Player {username} joined the campaign with {characterName}");
        
        //Update UI to show new player
        //e.g., PlayerListUI.Instance?.AddPlayer(username, characterName);
    }
    
    //HOST: When DM adds a character to a scene, sync this to all clients
    public void SyncCharacterAddedToScene(string sceneId, string characterId)
    {
        if (!IsServer) return;
        
        SyncCharacterAddedToSceneClientRpc(sceneId, characterId);
    }
    
    [ClientRpc]
    private void SyncCharacterAddedToSceneClientRpc(string sceneId, string characterId)
    {
        Debug.Log($"Character {characterId} added to scene {sceneId}");
        
        //Update client UI or spawn the character in the scene
        //GameSceneManager.Instance?.SpawnCharacterInScene(characterId);
    }
    
    //HOST: When DM removes a character from a scene, sync this to all clients
    public void SyncCharacterRemovedFromScene(string sceneId, string characterId)
    {
        if (!IsServer) return;
        
        SyncCharacterRemovedFromSceneClientRpc(sceneId, characterId);
    }
    
    [ClientRpc]
    private void SyncCharacterRemovedFromSceneClientRpc(string sceneId, string characterId)
    {
        Debug.Log($"Character {characterId} removed from scene {sceneId}");
        
        //Update client UI or despawn the character
        //GameSceneManager.Instance?.RemoveCharacterFromScene(characterId);
    }
    
    //CLIENT: Check if the local player's character is in the current scene
    //Not 100% necessary but could be useful for client-side logic - potentially remove it later
    public bool IsMyCharacterInScene(string sceneId)
    {
        ulong myNetworkId = NetworkManager.Singleton.LocalClientId;
        
        //Placeholder for now
        return false;
    }
}
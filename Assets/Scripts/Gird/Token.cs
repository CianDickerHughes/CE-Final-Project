using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Token : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    //Error handling for a default sprite if the character doesnt have one/doesnt load
    [Header("Default Visuals")]
    [SerializeField] private Sprite defaultSprite;

    [Header("Identification")]
    private EnemyData enemyData;
    private CharacterData characterData;
    private CharacterType characterType;
    private ulong ownerId;
    //Used to trach whichever character this token is representing
    //Can be useful for the player assignment and checking movement permissions etc.
    private string characterId;

    //Other variables
    private Tile currentTile;
    private SpriteRenderer spriteRenderer;
    private float pixelsPerUnit;

    void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (gridManager == null)
        {
            #if UNITY_2023_2_OR_NEWER
                        gridManager = FindAnyObjectByType<GridManager>();
            #else
                        gridManager = FindObjectOfType<GridManager>();
            #endif
        }
    }

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (gridManager == null)
        {
            #if UNITY_2023_2_OR_NEWER
                        gridManager = FindAnyObjectByType<GridManager>();
            #else
                        gridManager = FindObjectOfType<GridManager>();
            #endif
        }
        
        //Ensure we have a collider for click detection
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        //Size the collider to match a tile (1x1 unit)
        if (col is BoxCollider2D box)
        {
            box.size = new Vector2(1f, 1f);
        }
    }

    public void Initialize(CharacterData data, CharacterType type, Tile startTile)
    {
        characterData = data;
        characterType = type;
        currentTile = startTile;
        //Store the id of the character - so the token "knows" who it is
        characterId = data.id;
        
        // Set visual based on character
        if (data != null)
        {
            name = $"Token_{data.charName}";
            Debug.Log($"Token: Initializing token for character {data.charName} of type {type}.");
            //Actually setting up the sprite - has to be the characters saved token image
            if(!string.IsNullOrEmpty(data.tokenFileName))
            {
                // Try multiple locations for player characters
                string tokenPath = null;
                
                // Get campaign name - try CampaignManager first, fallback to SceneDataNetwork
                string campaignName = CampaignManager.Instance?.GetCurrentCampaign()?.campaignName;
                if (string.IsNullOrEmpty(campaignName) && SceneDataNetwork.Instance != null)
                {
                    campaignName = SceneDataNetwork.Instance.LastReceivedCampaignName;
                }
                Debug.Log($"Token: Looking for image '{data.tokenFileName}' with campaign '{campaignName}'");
                
                // Location 1: Main characters folder
                string mainFolder = CharacterIO.GetCharactersFolder();
                string mainPath = System.IO.Path.Combine(mainFolder, data.tokenFileName);
                if (System.IO.File.Exists(mainPath))
                {
                    tokenPath = mainPath;
                    Debug.Log($"Token: Found in main folder: {tokenPath}");
                }
                
                // Location 2: ReceivedCampaigns folder (for client-received images)
                if (tokenPath == null && !string.IsNullOrEmpty(campaignName))
                {
                    #if UNITY_EDITOR
                        string receivedPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Campaigns", "ReceivedCampaigns", campaignName, "Characters", data.tokenFileName);
                    #else
                        string receivedPath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Campaigns", "ReceivedCampaigns", campaignName, "Characters", data.tokenFileName);
                    #endif
                    
                    if (System.IO.File.Exists(receivedPath))
                    {
                        tokenPath = receivedPath;
                        Debug.Log($"Token: Found in ReceivedCampaigns: {tokenPath}");
                    }
                }
                
                // Location 3: Campaign Characters folder
                if (tokenPath == null && !string.IsNullOrEmpty(campaignName))
                {
                    string campaignFolder = CampaignManager.GetCampaignsFolder();
                    string campaignCharsPath = System.IO.Path.Combine(campaignFolder, campaignName, "Characters", data.tokenFileName);
                    if (System.IO.File.Exists(campaignCharsPath))
                    {
                        tokenPath = campaignCharsPath;
                        Debug.Log($"Token: Found in campaign Characters: {tokenPath}");
                    }
                }
                
                // Location 4: Campaign PlayerCharacters folder
                if (tokenPath == null && !string.IsNullOrEmpty(campaignName))
                {
                    string campaignFolder = CampaignManager.GetCampaignsFolder();
                    string playerCharsPath = System.IO.Path.Combine(campaignFolder, campaignName, "PlayerCharacters", data.tokenFileName);
                    if (System.IO.File.Exists(playerCharsPath))
                    {
                        tokenPath = playerCharsPath;
                        Debug.Log($"Token: Found in PlayerCharacters: {tokenPath}");
                    }
                }
                
                if (tokenPath == null)
                {
                    Debug.LogWarning($"Token: Could not find image '{data.tokenFileName}' in any location");
                }
                
                //Now actually trying to set up the sprite
                if (tokenPath != null)
                {
                    try{
                        //Extract the bytes from the loaded file and create a texture
                        byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                        Texture2D texture = new Texture2D(2,2);
                        texture.LoadImage(bytes);
                        //Calculate pixels per unit so the sprite fits exactly in 1 tile (1 unit)
                        //Use the larger dimension to ensure it fits within the tile
                        pixelsPerUnit = Mathf.Max(texture.width, texture.height);
                        //Using the sprite renderer to set the sprite
                        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0,0,texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Token: Failed to load token image from {tokenPath}: {ex.Message}");
                    }
                }
            }

            //If the sprite wont load then we use the default one
            if(spriteRenderer != null && spriteRenderer.sprite == null && defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }


            //Ensure token renders above tiles using sorting layer
            if(spriteRenderer != null){
                //Set sorting layer to "Tokens" - create this layer in Project Settings > Tags and Layers!
                spriteRenderer.sortingLayerName = "Tokens";
                spriteRenderer.sortingOrder = 0;
            }

            //Checking ig this player owns this character and applying a simple visual around it if so
            PlayerAssignmentHelper assignmentHelper = PlayerAssignmentHelper.Instance;
            if(assignmentHelper != null && assignmentHelper.IsMyCharacter(characterId))
            {
                //Applying a glow around the token to indicate ownership
                spriteRenderer.color = new Color(0.7f, 1f, 0.7f, 1f);
            }
        }
    }

    public void Initialize(EnemyData data, CharacterType type, Tile startTile)
    {
        enemyData = data;
        characterType = type;
        currentTile = startTile;

        // Set visual based on enemy
        if (data != null)
        {
            name = $"Token_{data.name}";
            Debug.Log($"Token: Initializing token for enemy {data.name} of type {type}.");
            //Actually setting up the sprite - has to be the enemies saved token image
            if(!string.IsNullOrEmpty(data.tokenFileName))
            {
                //Getting the token image from file
                string folder = CharacterIO.GetEnemiesFolder();
                string tokenPath = System.IO.Path.Combine(folder, data.tokenFileName);
                
                //Now actually trying to set up the sprite
                if (System.IO.File.Exists(tokenPath))
                {
                    try{
                        //Extract the bytes from the loaded file and create a texture
                        byte[] bytes = System.IO.File.ReadAllBytes(tokenPath);
                        Texture2D texture = new Texture2D(2,2);
                        texture.LoadImage(bytes);
                        //Calculate pixels per unit so the sprite fits exactly in 1 tile (1 unit)
                        //Use the larger dimension to ensure it fits within the tile
                        pixelsPerUnit = Mathf.Max(texture.width, texture.height);
                        //Using the sprite renderer to set the sprite
                        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0,0,texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Token: Failed to load token image from {tokenPath}: {ex.Message}");
                    }
                }
            }

            //If the sprite wont load then we use the default one
            if(spriteRenderer != null && spriteRenderer.sprite == null && defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }

            //Ensure token renders above tiles using sorting layer
            if(spriteRenderer != null){
                //Set sorting layer to "Tokens" - create this layer in Project Settings > Tags and Layers!
                spriteRenderer.sortingLayerName = "Tokens";
                spriteRenderer.sortingOrder = 0;
            }
        }
    }

    //Place token at a grid integer position (column, row).
    //Uses GridManager.GetTileAtPosition with integer Vector2 keys.
    public void PlaceAtGridPosition(Vector2Int gridPos)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("Token: No GridManager assigned or found in scene.");
            return;
        }

        var tile = gridManager.GetTileAtPosition(new Vector2(gridPos.x, gridPos.y));
        if (tile != null) MoveToTile(tile);
        else Debug.LogWarning($"Token: No tile at {gridPos.x},{gridPos.y}");
    }

    //Round the token's current world position to the nearest grid integer
    //coordinates and snap to that tile if available.
    public void SnapToNearestTile()
    {
        if (gridManager == null)
        {
            // Try modern fast API when available, otherwise fall back for older Unity versions
            #if UNITY_2023_2_OR_NEWER
            gridManager = FindAnyObjectByType<GridManager>();
            #else
            gridManager = FindObjectOfType<GridManager>();
            #endif
            if (gridManager == null)
            {
                Debug.LogWarning("Token: No GridManager assigned or found in scene.");
                return;
            }
        }

        Vector3 world = transform.position;
        Vector2 nearest = new Vector2(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        var tile = gridManager.GetTileAtPosition(nearest);
        if (tile != null) MoveToTile(tile);
        else Debug.LogWarning($"Token: No tile at {nearest}");
    }

    //Moves the token to the exact tile position and stores the reference.
    public void MoveToTile(Tile tile)
    {
        if (tile == null) return;
        currentTile = tile;
        // Match tile position exactly
        transform.position = tile.transform.position;
    }

    //Returns the tile the token is currently on (if any).
    public Tile GetCurrentTile() => currentTile;
    
    //Click to select this token
    void OnMouseDown()
    {
        //Don't select if in spawn mode
        if (TokenManager.Instance != null && TokenManager.Instance.IsInSpawnMode())
        {
            return; // Don't select tokens while in spawn mode
        }

        //Skip ownership checks for local testing (no network active)
        bool isLocalTesting = Unity.Netcode.NetworkManager.Singleton == null || 
            (!Unity.Netcode.NetworkManager.Singleton.IsClient && !Unity.Netcode.NetworkManager.Singleton.IsServer);
        
        if (!isLocalTesting)
        {
            PlayerAssignmentHelper assignmentHelper = PlayerAssignmentHelper.Instance;
            if (assignmentHelper != null){
                //Check if this token's character is assigned to this player - if not dont allow selection
                if(characterData != null && !assignmentHelper.CanControlCharacter(characterData.id))
                {
                    Debug.Log($"Token: Cant be selected for character {characterData.charName}");
                    return;
                }
            }
        }
            
        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.SelectToken(this);
        }
    }
    
    //Visual feedback for selection (simple color tint)
    private bool isSelected = false;
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (spriteRenderer != null)
        {
            //Tint the sprite when selected
            spriteRenderer.color = selected ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        }
    }

    //Getter for the character data the token represents
    public CharacterData getCharacterData()
    {
        return characterData;
    }
    
    //Getter for the enemy data the token represents
    public EnemyData getEnemyData()
    {
        return enemyData;
    }
    
    //Getter for the character type (Player, NPC, or Enemy)
    public CharacterType getCharacterType()
    {
        return characterType;
    }
}
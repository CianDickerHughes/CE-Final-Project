using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    //FOR NETWORKING CIAN - SO WE CAN TRACK WHO OWNS WHAT TOKEN
    private ulong ownerId;

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
    }

    public void Initialize(CharacterData data, CharacterType type, Tile startTile)
    {
        characterData = data;
        characterType = type;
        currentTile = startTile;
        
        // Set visual based on character
        if (data != null)
        {
            name = $"Token_{data.charName}";
            Debug.Log($"Token: Initializing token for character {data.charName} of type {type}.");
            //Actually setting up the sprite - has to be the characters saved token image
            if(!string.IsNullOrEmpty(data.tokenFileName))
            {
                //Getting the token image from file - need to change this behavior when loading the players characters
                //This should work for dm player characters though
                string folder = CharacterIO.GetCharactersFolder();
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
}
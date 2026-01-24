using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Token : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Identification")]
    private CharacterData characterData;
    private CharacterType characterType;
    //FOR NETWORKING CIAN - SO WE CAN TRACK WHO OWNS WHAT TOKEN
    private ulong ownerId;

    private Tile currentTile;
    private SpriteRenderer spriteRenderer;

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

    public void Inialize(CharacterData data, CharacterType type, Tile startTile)
    {
        characterData = data;
        characterType = type;
        currentTile = startTile;
        
        // Set visual based on character
        if (data != null)
        {
            name = $"Token_{data.charName}";
            Debug.Log($"Token: Initializing token for character {data.charName} of type {type}.");
            //TODO: Load sprite from data.tokenFileName
        }
    }

    /// <summary>
    /// Place token at a grid integer position (column, row).
    /// Uses GridManager.GetTileAtPosition with integer Vector2 keys.
    /// </summary>
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

    /// <summary>
    /// Round the token's current world position to the nearest grid integer
    /// coordinates and snap to that tile if available.
    /// </summary>
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

    /// <summary>
    /// Moves the token to the exact tile position and stores the reference.
    /// </summary>
    public void MoveToTile(Tile tile)
    {
        if (tile == null) return;
        currentTile = tile;
        // Match tile position exactly
        transform.position = tile.transform.position;
    }

    /// <summary>
    /// Returns the tile the token is currently on (if any).
    /// </summary>
    public Tile GetCurrentTile() => currentTile;
}

//Tracking what "Type" of character this token represents - who are they
public enum CharacterType
{
    Player,
    NPC,
    Enemy
}
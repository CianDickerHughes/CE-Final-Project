using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple Token that can be placed on the grid. It provides helper APIs to
/// snap/move the token to tiles managed by `GridManager`.
///
/// Usage:
/// - Add this script to a circular token GameObject (SpriteRenderer).
/// - Assign `GridManager` in the inspector or let it auto-find one in the scene.
/// - Call `PlaceAtGridPosition(new Vector2Int(x,y))` or `SnapToNearestTile()` to snap the token.
/// </summary>
public class Token : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: assign your GridManager. If left empty the script will try FindObjectOfType<GridManager>()")]
    [SerializeField] private GridManager _gridManager;

    private Tile _currentTile;
    private SpriteRenderer _spriteRenderer;

    void OnValidate()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_gridManager == null)
        {
#if UNITY_2023_2_OR_NEWER
            _gridManager = FindAnyObjectByType<GridManager>();
#else
            _gridManager = FindObjectOfType<GridManager>();
#endif
        }
    }

    void Awake()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_gridManager == null)
        {
#if UNITY_2023_2_OR_NEWER
            _gridManager = FindAnyObjectByType<GridManager>();
#else
            _gridManager = FindObjectOfType<GridManager>();
#endif
        }
    }

    /// <summary>
    /// Place token at a grid integer position (column, row).
    /// Uses GridManager.GetTileAtPosition with integer Vector2 keys.
    /// </summary>
    public void PlaceAtGridPosition(Vector2Int gridPos)
    {
        if (_gridManager == null)
        {
            Debug.LogWarning("Token: No GridManager assigned or found in scene.");
            return;
        }

        var tile = _gridManager.GetTileAtPosition(new Vector2(gridPos.x, gridPos.y));
        if (tile != null) MoveToTile(tile);
        else Debug.LogWarning($"Token: No tile at {gridPos.x},{gridPos.y}");
    }

    /// <summary>
    /// Round the token's current world position to the nearest grid integer
    /// coordinates and snap to that tile if available.
    /// </summary>
    public void SnapToNearestTile()
    {
        if (_gridManager == null)
        {
            // Try modern fast API when available, otherwise fall back for older Unity versions
            #if UNITY_2023_2_OR_NEWER
            _gridManager = FindAnyObjectByType<GridManager>();
            #else
            _gridManager = FindObjectOfType<GridManager>();
            #endif
            if (_gridManager == null)
            {
                Debug.LogWarning("Token: No GridManager assigned or found in scene.");
                return;
            }
        }

        Vector3 world = transform.position;
        Vector2 nearest = new Vector2(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        var tile = _gridManager.GetTileAtPosition(nearest);
        if (tile != null) MoveToTile(tile);
        else Debug.LogWarning($"Token: No tile at {nearest}");
    }

    /// <summary>
    /// Moves the token to the exact tile position and stores the reference.
    /// </summary>
    public void MoveToTile(Tile tile)
    {
        if (tile == null) return;
        _currentTile = tile;
        // Match tile position exactly
        transform.position = tile.transform.position;
    }

    /// <summary>
    /// Returns the tile the token is currently on (if any).
    /// </summary>
    public Tile GetCurrentTile() => _currentTile;
}

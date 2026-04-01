using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 /// <summary>
 /// Represents a single tile in the grid. It can be initialized with different colors 
 /// based on its position (offset or base) and provides visual feedback on mouse hover.
 /// </summary>
 
[RequireComponent(typeof(SpriteRenderer))]
//Tile supports either BoxCollider2D or BoxCollider for mouse interaction.
public class Tile : MonoBehaviour {
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color offsetColor = Color.gray;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject highlight;
    [SerializeField] private bool useTileColorTint = false;
    [SerializeField] private Vector2 tileWorldSize = Vector2.one;

    //Store what type of tile this is
    private TileType tileType;
    //Storing position of tile in the grid
    private int gridX, gridY;
    //Referencing the grid manager
    private GridManager gridManager;
    //Tracking the mouse when painting
    private static bool isMouseHeld;

    void Reset()
    {
        EnsureRequiredComponents();
    }

    void OnValidate()
    {
        EnsureRequiredComponents();

        if (tileWorldSize.x <= 0f) tileWorldSize.x = 1f;
        if (tileWorldSize.y <= 0f) tileWorldSize.y = 1f;

        // Prevent accidental fully-transparent colors from the inspector
        if (baseColor.a <= 0f) baseColor.a = 1f;
        if (offsetColor.a <= 0f) offsetColor.a = 1f;
    }

    void Awake()
    {
        EnsureRequiredComponents();

        // Hide highlight by default (safe)
        if (highlight != null)
            highlight.SetActive(false);
        
        ApplyTileSizing();
    }

    //Update method
    // Remove the PaintTile call from Update() - just keep the mouse tracking:
    void Update(){
        if (Input.GetMouseButtonDown(0))
        {
            isMouseHeld = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isMouseHeld = false;
        }
    }

    //Add OnMouseDown for clicking:
    void OnMouseDown()
    {
        //Check for token spawning mode first
        if (TokenManager.Instance != null && TokenManager.Instance.IsInSpawnMode())
        {
            TokenManager.Instance.TrySpawnAtTile(this);
            return;
        }
        
        //Checking to see if we're in the player assignment phase
        if (TokenManager.Instance != null && TokenManager.Instance.HasSelectedToken())
        {
            TokenManager.Instance.TryMoveSelectedTokenToTile(this);
            return;
        }
        
        //Checking if we're in edit mode
        if (gridManager != null && gridManager.IsEditMode)
        {
            gridManager.PaintTile(this);
        }
    }

    void OnMouseExit()
    {
        if (highlight != null) highlight.SetActive(false);
    }

    //Update OnMouseEnter for drag-painting:
    void OnMouseEnter() {
        if (highlight != null){
            highlight.SetActive(true);
        }
        
        // Drag-paint support
        if (isMouseHeld && gridManager != null && gridManager.IsEditMode)
        {
            gridManager.PaintTile(this);
        }
    }
 
    public void Init(bool isOffset) {
        if (spriteRenderer == null) {
            Debug.LogWarning($"Tile ({name}) has no SpriteRenderer assigned.");
            return;
        }

        // Only apply checkerboard tint if explicitly enabled.
        if (useTileColorTint)
        {
            Color c = isOffset ? offsetColor : baseColor;
            if (c.a <= 0f) c.a = 1f;
            spriteRenderer.color = c;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    //Another Init method to set grid position and reference to GridManager
    public void Init(int x, int y, GridManager manager, bool useCheckerboard)
    {
        gridX = x;
        gridY = y;
        gridManager = manager;
        tileType = TileType.Floor; // Default type
        Init(useCheckerboard ? ((x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0)) : false);
        UpdateVisuals();
    }

    //Get or set the current tile type.
    public TileType CurrentTileType
    {
        get { return tileType; }
        set { tileType = value; }
    }

    //Setting the tile type and updating visuals.
    public void SetTileType(TileType type)
    {
        CurrentTileType = type;
        UpdateVisuals();
    }

    //Getters and setters for grid position.
    public int GridX
    {
        get { return gridX; }
        set { gridX = value; }
    }

    public int GridY
    {
        get { return gridY; }
        set { gridY = value; }
    }

    //Method to update the visuals based on tile type.
    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // Set the sprite for texture
            spriteRenderer.sprite = TileTypeProperties.GetSprite(tileType);
            ApplyTileSizing();

            if (useTileColorTint)
            {
                spriteRenderer.color = TileTypeProperties.GetColor(tileType);
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }
    }

    private void ApplyTileSizing()
    {
        if (spriteRenderer != null)
        {
            // Use Simple draw mode to avoid Full Rect import warnings.
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            Vector2 spriteSize = GetSpriteSizeOrDefault();
            if (spriteSize.x > Mathf.Epsilon && spriteSize.y > Mathf.Epsilon)
            {
                transform.localScale = new Vector3(tileWorldSize.x / spriteSize.x, tileWorldSize.y / spriteSize.y, 1f);
            }
        }

        BoxCollider2D collider2D = GetComponent<BoxCollider2D>();
        if (collider2D != null)
        {
            collider2D.size = GetSpriteSizeOrDefault();
            collider2D.offset = Vector2.zero;
            return;
        }

        BoxCollider collider3D = GetComponent<BoxCollider>();
        if (collider3D != null)
        {
            Vector2 spriteSize = GetSpriteSizeOrDefault();
            collider3D.size = new Vector3(spriteSize.x, spriteSize.y, 0.2f);
            collider3D.center = Vector3.zero;
        }
    }

    private Vector2 GetSpriteSizeOrDefault()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x > Mathf.Epsilon && spriteSize.y > Mathf.Epsilon)
            {
                return spriteSize;
            }
        }

        return Vector2.one;
    }

    private void EnsureRequiredComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        bool has2DCollider = GetComponent<BoxCollider2D>() != null;
        bool has3DCollider = GetComponent<BoxCollider>() != null;

        if (!has2DCollider && !has3DCollider)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
        else if (!has2DCollider && has3DCollider)
        {
            // A 3D collider already exists; keep it to avoid collider-type conflicts.
        }
    }

    //Helper methods
    public bool IsWalkable()
    {
        return TileTypeProperties.IsWalkable(tileType);
    }

    //Get movement cost for this tile.
    public int GetMovementCost()
    {
        return TileTypeProperties.GetMovementCost(tileType);
    }
}
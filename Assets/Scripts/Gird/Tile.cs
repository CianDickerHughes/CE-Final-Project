using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 /// <summary>
 /// Represents a single tile in the grid. It can be initialized with different colors 
 /// based on its position (offset or base) and provides visual feedback on mouse hover.
 /// </summary>
 
[RequireComponent(typeof(SpriteRenderer))]
//This allows us to specify that a BoxCollider2D component is required on the same GameObject.
[RequireComponent(typeof(BoxCollider2D))]
public class Tile : MonoBehaviour {
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color offsetColor = Color.gray;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject highlight;

    //Store what type of tile this is
    private TileType tileType;
    //Storing position of tile in the grid
    private int gridX, gridY;
    //Referencing the grid manager
    private GridManager gridManager;
    //Tracking the mouse when painting
    private static bool isMouseHeld;

    void OnValidate()
    {
        // Auto-assign in editor if not set
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevent accidental fully-transparent colors from the inspector
        if (baseColor.a <= 0f) baseColor.a = 1f;
        if (offsetColor.a <= 0f) offsetColor.a = 1f;
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Hide highlight by default (safe)
        if (highlight != null)
            highlight.SetActive(false);
        
        //Setting up the BoxCollider2D to match the sprite size
        //Making sure it exists with a size of (1,1) if no sprite is assigned
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            if (spriteRenderer.sprite != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
            }
            else
            {
                collider.size = new Vector2(1f, 1f);
            }
        }
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
        //Debug logging to diagnose click issues on different tile types
        Debug.Log($"Tile clicked: ({gridX}, {gridY}), Type: {tileType}, Walkable: {IsWalkable()}");
        
        //Check for token spawning mode - maybe find some other way to do this but not sure
        if(GameplayManager.Instance != null && GameplayManager.Instance.IsInSpawnMode())
        {
            Debug.Log($"Attempting to spawn at tile ({gridX}, {gridY})");
            GameplayManager.Instance.TrySpawnAtTile(this);
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

        // Choose color and ensure visible alpha
        Color c = isOffset ? offsetColor : baseColor;
        if (c.a <= 0f) c.a = 1f;
        spriteRenderer.color = c;
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
            spriteRenderer.color = TileTypeProperties.GetColor(tileType);
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
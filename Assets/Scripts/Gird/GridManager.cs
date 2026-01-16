using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
 //Manages a grid of tiles, allowing for dynamic generation and resizing.
 //Also handles spawning tokens at specified grid positions - when we get to that point
 
public class GridManager : MonoBehaviour {
    [Header("Grid Settings")]
    [SerializeField] private int width, height;
 
    [SerializeField] private Tile tilePrefab;
 
    [SerializeField] private Transform cam;
    
    //SUBJECT TO CHANGE - MAYBE TOKENS WILL BE HANDLED ELSEWHERE (MOST LIKELY FOR SOMETHING LIKE DM CONTROL SCRIPT OR SOMETHING LIKE THAT)
    [Header("Token")]
    [SerializeField] private Token tokenPrefab;
    [SerializeField] private UnityEngine.Vector2Int tokenStartPosition = new UnityEngine.Vector2Int(0, 0);
    [SerializeField] private UnityEngine.UI.Button spawnTokenButton;

    private Token activeToken;
    //Sequential counter for naming spawned tokens (Token 1, Token 2, ...)
    private int tokenCounter = 0;

    //Tracking to see if the map is in edit mode
    public bool IsEditMode;
    //Variable to handle what type we're painting with
    private TileType currentPaintType;

    //Tracking the tiles in a dictionary for easy access - what tiles in the map
    private Dictionary<Vector2, Tile> tiles;
    
    [Header("UI References")]
    public TMP_InputField columnsInput;
    public TMP_InputField rowsInput;
    public Button resizeButton;
 
    void Start() {
        //NOT WORKING - SPAWING STUFF ISNT WORKING/BEGUN IMPLEMENTATION
        //Wire spawn button if assigned - clicking will spawn a token at the configured start position
        if (spawnTokenButton != null)
        {
            spawnTokenButton.onClick.AddListener(() => SpawnTokenAt(tokenStartPosition));
        }

        if (resizeButton != null)
        {
            resizeButton.onClick.AddListener(OnResizeButtonClicked);
        }
    }
 
    void GenerateGrid() {
        //Clear any existing tiles first 
        ClearAllTiles();
        //Then generate new tiles
        //Starting from (0,0) to (width-1, height-1)
        tiles = new Dictionary<Vector2, Tile>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                //Parent tiles under GridManager for easy management
                var spawnedTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                spawnedTile.name = $"Tile {x} {y}";
 
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(x, y, this, isOffset);
 
                tiles[new Vector2(x, y)] = spawnedTile;
            }
        }
 
        if (cam != null)
        {
            //Center camera based on grid size
            cam.transform.position = new Vector3((float)width/2 -0.5f, (float)height / 2 - 0.5f,-10);
        }
    }
    
    //Destroys ALL tile children - helps to make sure theres no lingering tiles when resizing as this previously caused a visual bug
    private void ClearAllTiles()
    {
        // First clear dictionary tracked tiles
        if (tiles != null)
        {
            foreach (var tile in tiles.Values)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
            tiles.Clear();
        }
        
        //Then destroy ANY remaining child objects (catches orphaned tiles)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    public Tile GetTileAtPosition(Vector2 pos)
    {
        //Attempt to get tile at specified position
        if (tiles.TryGetValue(pos, out var tile)) 
        {
            return tile;
        }
        //Otherwise return null if not found
        return null;
    }

    //WIP - MAYBE CHANGE LATER
    //Meant to spawn a token at a specific grid position - probably will remove this in favour of "drag and drop" system
    public Token SpawnTokenAt(UnityEngine.Vector2Int gridPos)
    {
        if (tokenPrefab == null)
        {
            Debug.LogWarning("GridManager: No Token prefab assigned.");
            return null;
        }

        UnityEngine.Vector2 key = new UnityEngine.Vector2(gridPos.x, gridPos.y);
        Tile tile = GetTileAtPosition(key);
        UnityEngine.Vector3 spawnPos = tile != null ? tile.transform.position : new UnityEngine.Vector3(gridPos.x, gridPos.y);
        //Increment counter and use sequential naming for tokens
        tokenCounter++;
        var token = Instantiate(tokenPrefab, spawnPos, Quaternion.identity);
        token.name = $"Token {tokenCounter}";
        if (tile != null) token.MoveToTile(tile);

        activeToken = token;
        return token;
    }

    public void OnSpawnTokenButtonClicked()
    {
        SpawnTokenAt(tokenStartPosition);
    }

    public Token GetActiveToken() => activeToken;
    
    //Called when the resize button is clicked
    //Literally just parses input and calls ResizeGrid
    //Changed from previous version to fit new resizing system
    void OnResizeButtonClicked()
    {
        if (columnsInput != null && rowsInput != null)
        {
            // Parse input safely
            if (int.TryParse(columnsInput.text, out width) && int.TryParse(rowsInput.text, out height))
            {
                ResizeGrid(width, height);
            }
            else
            {
                Debug.LogWarning("Invalid input for columns or rows!");
            }
        }
    }

    //TEST METHOD AGAINST CREATED SCENE DATA - ENSURE CONSISTENCY IN RESIZING
    //New method to resize the grid
    //New Version - preserves existing tiles where possible and prevents some visual bugs
    public void ResizeGrid(int newColumns, int newRows)
    {
        //Validate minimum size
        width = Mathf.Max(1, newColumns);
        height = Mathf.Max(1, newRows);

        //ClearAllTiles is now called inside GenerateGrid()
        //Generate new grid with new dimensions
        GenerateGrid();
        
        Debug.Log($"Grid resized to {width}x{height}");
    }

    //Getter for the current paint type
    public TileType GetCurrentPaintType()
    {
        return currentPaintType;
    }

    //Width and height getters for dimensions
    public int GetWidth()
    {
        return width;
    }
    public int GetHeight()
    {
        return height;
    }

    //Edit mode methods 
    public void SetEditMode(bool enabled)
    {
        IsEditMode = enabled;
    }

    public void SetCurrentPaintType(TileType type)
    {
        currentPaintType = type;
    }

    public void SetPaintTypeFromInt(int index){
        currentPaintType = (TileType)index;
    }

    public void PaintTile(Tile tile)
    {
        if (tile != null)
        {
            tile.SetTileType(currentPaintType);
        }
    }

    public void ClearMap()
    {
        foreach (var tile in tiles.Values)
        {
            tile.SetTileType(TileType.Floor);
        }
    }

    public void FillMap(TileType type)
    {
        foreach (var tile in tiles.Values)
        {
            tile.SetTileType(type);
        }
    }

    //Method to save the map data to a CampaignData object
    public MapData SaveMapData(){
        MapData mapData = new MapData(width, height);
        int nonFloorCount = 0;
        foreach (var kvp in tiles)
        {
            Vector2 pos = kvp.Key;
            Tile tile = kvp.Value;
            TileType type = tile.CurrentTileType;
            mapData.SetTileAt((int)pos.x, (int)pos.y, type);
            if (type != TileType.Floor) nonFloorCount++;
        }
        Debug.Log($"SaveMapData: Saved {width}x{height} grid with {nonFloorCount} non-floor tiles");
        return mapData;
    }

    public void LoadMapData(MapData mapData)
    {
        if (mapData == null) {
            Debug.LogWarning("LoadMapData: mapData is null!");
            return;
        }
        
        if (mapData.tiles == null || mapData.tiles.Length == 0) {
            Debug.LogWarning($"LoadMapData: mapData.tiles is null or empty! width={mapData.width}, height={mapData.height}");
            return;
        }

        Debug.Log($"LoadMapData: Loading {mapData.width}x{mapData.height} map with {mapData.tiles.Length} tile values");

        //Automatically resize grid to match map data dimensions
        //Happens on initialization of a scene
        ResizeGrid(mapData.width, mapData.height);

        int nonFloorCount = 0;
        foreach (var kvp in tiles)
        {
            Vector2 pos = kvp.Key;
            Tile tile = kvp.Value;
            TileType type = mapData.GetTileAt((int)pos.x, (int)pos.y);
            tile.SetTileType(type);
            if (type != TileType.Floor) nonFloorCount++;
        }
        Debug.Log($"LoadMapData: Applied {nonFloorCount} non-floor tiles to grid");
    }

    //Method to initialize the grid
    public void InitializeGrid(int gridWidth, int gridHeight)
    {
        // Validate minimum size
        width = Mathf.Max(1, gridWidth);
        height = Mathf.Max(1, gridHeight);
        
        // ClearAllTiles is now called inside GenerateGrid()
        GenerateGrid();
        
        Debug.Log($"Grid initialized to {width}x{height}");
    }

}
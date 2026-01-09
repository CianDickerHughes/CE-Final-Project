using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for UI components
 
 /// <summary>
 /// Manages a grid of tiles, allowing for dynamic generation and resizing.
 /// Also handles spawning tokens at specified grid positions.
 /// </summary>
 
public class GridManager : MonoBehaviour {
    [SerializeField] private int width, height;
 
    [SerializeField] private Tile tilePrefab;
 
    [SerializeField] private Transform cam;
    
    [Header("Token")]
    [SerializeField] private Token tokenPrefab;
    [SerializeField] private UnityEngine.Vector2Int tokenStartPosition = new UnityEngine.Vector2Int(0, 0);
    [SerializeField] private UnityEngine.UI.Button spawnTokenButton;

    private Token activeToken;
    // Sequential counter for naming spawned tokens (Token 1, Token 2, ...)
    private int tokenCounter = 0;

    //Tracking to see if the map is in edit mode
    public bool IsEditMode;
    //Variable to handle what type we're painting with
    private TileType currentPaintType;

    private Dictionary<Vector2, Tile> tiles;
    
    [Header("UI References")]
    public InputField columnsInput;
    public InputField rowsInput;
    public Button resizeButton;
 
    void Start() {
        GenerateGrid();

        // Wire spawn button if assigned - clicking will spawn a token at the configured start position
        if (spawnTokenButton != null)
        {
            spawnTokenButton.onClick.AddListener(() => SpawnTokenAt(tokenStartPosition));
        }

        if (resizeButton != null)
            resizeButton.onClick.AddListener(OnResizeButtonClicked);
    }
 
    void GenerateGrid() {
        tiles = new Dictionary<Vector2, Tile>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var spawnedTile = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
 
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(x, y, this, isOffset);
 
 
                tiles[new Vector2(x, y)] = spawnedTile;
            }
        }
 
        cam.transform.position = new Vector3((float)width/2 -0.5f, (float)height / 2 - 0.5f,-10);
    }

    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (tiles.TryGetValue(pos, out var tile)) return tile;
        return null;
    }

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
    // Increment counter and use sequential naming for tokens
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
    
    // Called when the resize button is clicked
    void OnResizeButtonClicked()
    {
        if (columnsInput != null && rowsInput != null)
        {
            // Parse input safely
            if (int.TryParse(columnsInput.text, out width) &&
                int.TryParse(rowsInput.text, out height))
            {
                ResizeGrid(width, height);
            }
            else
            {
                Debug.LogWarning("Invalid input for columns or rows!");
            }
        }
    }

    // New method to resize the grid
   public void ResizeGrid(int newColumns, int newRows)
    {
        width = newColumns;
        height = newRows;

        // Destroy old tiles
        if (tiles != null)
        {
            foreach (var tile in tiles.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            tiles.Clear();
        }

        // Generate new grid
        GenerateGrid();
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
        foreach (var kvp in tiles)
        {
            Vector2 pos = kvp.Key;
            Tile tile = kvp.Value;
            mapData.SetTileAt((int)pos.x, (int)pos.y, tile.CurrentTileType);
        }
        return mapData;
    }

    public void LoadMapData(MapData mapData)
    {
        if (mapData == null) {
            return;
        }

        //Automatically resize grid to match map data dimensions
        //Happens on initialization of a scene
        ResizeGrid(mapData.width, mapData.height);

        foreach (var kvp in tiles)
        {
            Vector2 pos = kvp.Key;
            Tile tile = kvp.Value;
            TileType type = mapData.GetTileAt((int)pos.x, (int)pos.y);
            tile.SetTileType(type);
        }
    }

    //Method to initialize the grid
    public void InitializeGrid(int gridWidth, int gridHeight)
    {
        //Destroy existing tiles if any
        if (tiles != null)
        {
            foreach (var tile in tiles.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            tiles.Clear();
        }
        //Then setting up the tiles and generating the grid
        width = gridWidth;
        height = gridHeight;
        GenerateGrid();
    }

}
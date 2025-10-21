using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for UI components
 
public class GridManager : MonoBehaviour {
    [SerializeField] private int _width, _height;
 
    [SerializeField] private Tile _tilePrefab;
 
    [SerializeField] private Transform _cam;

    private Dictionary<Vector2, Tile> _tiles;
    
    [Header("UI References")]
    public InputField columnsInput;
    public InputField rowsInput;
    public Button resizeButton;
 
    void Start() {
        GenerateGrid();

        if (resizeButton != null)
            resizeButton.onClick.AddListener(OnResizeButtonClicked);
    }
 
    void GenerateGrid() {
        _tiles = new Dictionary<Vector2, Tile>();
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
 
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(isOffset);
 
 
                _tiles[new Vector2(x, y)] = spawnedTile;
            }
        }
 
        _cam.transform.position = new Vector3((float)_width/2 -0.5f, (float)_height / 2 - 0.5f,-10);
    }

    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (_tiles.TryGetValue(pos, out var tile)) return tile;
        return null;
    }
    
    // Called when the resize button is clicked
    void OnResizeButtonClicked()
    {
        if (columnsInput != null && rowsInput != null)
        {
            // Parse input safely
            if (int.TryParse(columnsInput.text, out _width) &&
                int.TryParse(rowsInput.text, out _height))
            {
                ResizeGrid(_width, _height);
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
        _width = newColumns;
        _height = newRows;

        // Destroy old tiles
        if (_tiles != null)
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            _tiles.Clear();
        }

        // Generate new grid
        GenerateGrid();
    }


}
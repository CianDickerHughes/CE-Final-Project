using UnityEngine;

//Holds the map/grid layout data for a scene.
//This is what gets saved/loaded when the DM creates a map in the Scene Maker.
//Stores tile types as integers for easy JSON serialization.
[System.Serializable]
public class MapData
{
    public int width;
    public int height;
    public int[] tiles; // Flattened 2D array of TileType values
    public string createdDate;
    public string lastModified;

    //Default constructor - creates an empty 15x15 map.
    public MapData()
    {
        width = 15;
        height = 15;
        tiles = new int[width * height];
        // Initialize all tiles to Floor by default
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastModified = createdDate;
    }

    //Create a map with specific dimensions.
    public MapData(int mapWidth, int mapHeight)
    {
        width = mapWidth;
        height = mapHeight;
        tiles = new int[width * height];
        // Initialize all tiles to Floor by default
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastModified = createdDate;
    }

    //Get the tile type at a specific grid position.
    //Returns Empty if out of bounds.
    public TileType GetTileAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return TileType.Empty;

        int index = y * width + x;
        return (TileType)tiles[index];
    }

    //Set the tile type at a specific grid position.
    public void SetTileAt(int x, int y, TileType type)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;
        tiles[index] = (int)type;
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Resize the map, preserving existing tiles where possible.
    public void Resize(int newWidth, int newHeight)
    {
        int[] newTiles = new int[newWidth * newHeight];
        
        // Initialize new tiles to Floor
        for (int i = 0; i < newTiles.Length; i++)
        {
            newTiles[i] = (int)TileType.Floor;
        }

        // Copy existing tiles that fit in new dimensions
        int copyWidth = Mathf.Min(width, newWidth);
        int copyHeight = Mathf.Min(height, newHeight);

        for (int y = 0; y < copyHeight; y++)
        {
            for (int x = 0; x < copyWidth; x++)
            {
                int oldIndex = y * width + x;
                int newIndex = y * newWidth + x;
                newTiles[newIndex] = tiles[oldIndex];
            }
        }

        width = newWidth;
        height = newHeight;
        tiles = newTiles;
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Clear all tiles back to Floor.
    public void Clear()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)TileType.Floor;
        }
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Fill the entire map with a specific tile type.
    public void Fill(TileType type)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = (int)type;
        }
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Check if a position is within the map bounds.
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //Check if a position is walkable (valid and not blocked).
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;

        TileType type = GetTileAt(x, y);
        return TileTypeProperties.IsWalkable(type);
    }
}
using UnityEngine;

//Defines all available tile types for the map editor.
//Each tile type has visual properties and gameplay attributes.

//TO GO BACK TO THE ORIGINAL FLAT COLORS MAKE THE FOLLOWING CHANGES
/*
1. Delete the section below about "GetSprite"
2. Go to Tile.cs and delete things relating to the sprite attribute/image relating to the prefab
*/
public enum TileType
{
    Empty = 0,      // Default/void - nothing here
    Floor = 1,      // Walkable floor tile
    Wall = 2,       // Impassable wall
    Water = 3,      // Water - may slow movement or block
    Door = 4,       // Door - can be opened/closed
    Grass = 5,      // Outdoor grass terrain
    Stone = 6,      // Stone floor (dungeon)
    Wood = 7,       // Wooden floor (tavern/house)
    Lava = 8        // Just a normal lava space - could count as difficult or dangerous terrain
}

//Static helper class providing properties and colors for each tile type.
//Use this to get consistent visuals and gameplay rules across the app.
public static class TileTypeProperties
{

    //Get the display color for a tile type in the map editor and gameplay.
    public static Color GetColor(TileType type)
    {
        switch (type)
        {
            case TileType.Empty:
                return new Color(0.1f, 0.1f, 0.1f, 0.5f);   // Dark transparent
            case TileType.Floor:
                return new Color(0.76f, 0.7f, 0.5f, 1f);    // Tan/beige
            case TileType.Wall:
                return new Color(0.3f, 0.3f, 0.3f, 1f);     // Dark gray
            case TileType.Water:
                return new Color(0.2f, 0.5f, 0.8f, 1f);     // Blue
            case TileType.Door:
                return new Color(0.6f, 0.4f, 0.2f, 1f);     // Brown
            case TileType.Grass:
                return new Color(0.3f, 0.6f, 0.3f, 1f);     // Green
            case TileType.Stone:
                return new Color(0.5f, 0.5f, 0.55f, 1f);    // Gray
            case TileType.Wood:
                return new Color(0.65f, 0.45f, 0.25f, 1f);  // Wood brown
            case TileType.Lava:
                return new Color(0.6f, 0.55f, 0.4f, 1f);    // Muddy tan
            default:
                return Color.magenta; // Error indicator
        }
    }

    //Get the sprite for a tile type.
    //Sprites should be placed in Assets/Resources/Tiles/ folder
    //Still not 100% on how i feel about how this looks but we can easily change things around to back to being normal colours
    public static Sprite GetSprite(TileType type)
    {
        string spriteName = "";
        switch (type)
        {
            case TileType.Empty:
                spriteName = "EmptyMap";
                break;
            case TileType.Floor:
                spriteName = "FloorMap1";
                break;
            case TileType.Wall:
                spriteName = "WallMap2";
                break;
            case TileType.Water:
                spriteName = "WaterMap1";
                break;
            case TileType.Door:
                spriteName = "DoorMap2";
                break;
            case TileType.Grass:
                spriteName = "GrassMap";
                break;
            case TileType.Stone:
                spriteName = "StoneMap";
                break;
            case TileType.Wood:
                spriteName = "WoodMap";
                break;
            case TileType.Lava:
                spriteName = "LavaMap";
                break;
            default:
                spriteName = "FloorMap1";
                break;
        }
        
        // Load sprite from Resources/Tiles/ folder
        Sprite sprite = Resources.Load<Sprite>("Tiles/" + spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"Tile sprite '{spriteName}' not found in Resources/Tiles/. Using default.");
            // Return a default sprite or null
            return Resources.Load<Sprite>("Tiles/FloorMap3");
        }
        return sprite;
    }

    //Check if a tile type is walkable (tokens can move through it).
    public static bool IsWalkable(TileType type)
    {
        switch (type)
        {
            case TileType.Wall:
                return false;
            case TileType.Water:
                return false; // Can change to true if you want swimming
            case TileType.Empty:
                return false;
            default:
                return true;
        }
    }

    //Get movement cost multiplier for a tile type.
    //Normal = 1, Difficult terrain = 2, Impassable = -1
    public static int GetMovementCost(TileType type)
    {
        switch (type)
        {
            case TileType.Wall:
            case TileType.Empty:
                return -1; // Impassable
            case TileType.Water:
                return -1; // Impassable (change to 2 if swimming allowed)
            case TileType.Lava:
                return 2;  // Costs double movement
            default:
                return 1;  // Normal movement
        }
    }

    //Check if a tile blocks line of sight (for fog of war, targeting, etc.)
    public static bool BlocksLineOfSight(TileType type)
    {
        switch (type)
        {
            case TileType.Wall:
                return true;
            case TileType.Door:
                return true; // Closed doors block sight (could add open/closed state later)
            default:
                return false;
        }
    }

    //Get display name for UI dropdowns and tooltips.
    public static string GetDisplayName(TileType type)
    {
        switch (type)
        {
            case TileType.Empty:
                return "Empty";
            case TileType.Floor:
                return "Floor";
            case TileType.Wall:
                return "Wall";
            case TileType.Water:
                return "Water";
            case TileType.Door:
                return "Door";
            case TileType.Grass:
                return "Grass";
            case TileType.Stone:
                return "Stone Floor";
            case TileType.Wood:
                return "Wooden Floor";
            case TileType.Lava:
                return "Difficult Terrain";
            default:
                return "Unknown";
        }
    }
}

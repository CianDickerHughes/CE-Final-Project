using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 8;
    public int rows = 8;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("Cell Prefab not assigned!");
            return;
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                Instantiate(cellPrefab, position, Quaternion.identity, transform);
            }
        }
    }
}

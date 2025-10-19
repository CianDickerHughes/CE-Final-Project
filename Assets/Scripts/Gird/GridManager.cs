using UnityEngine;
using UnityEngine.UI; // Needed for UI components

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 8;
    public int rows = 8;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("UI References")]
    public InputField columnsInput;
    public InputField rowsInput;
    public Button resizeButton;

    void Start()
    {
        GenerateGrid();

        // Setup button listener
        if (resizeButton != null)
            resizeButton.onClick.AddListener(OnResizeButtonClicked);

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

    // Called when the resize button is clicked
    void OnResizeButtonClicked()
    {
        if (columnsInput != null && rowsInput != null)
        {
            int newColumns;
            int newRows;

            // Parse input safely
            if (int.TryParse(columnsInput.text, out newColumns) &&
                int.TryParse(rowsInput.text, out newRows))
            {
                ResizeGrid(newColumns, newRows);
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
        columns = newColumns;
        rows = newRows;

        // Destroy old grid
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Generate new grid
        GenerateGrid();
    }
}
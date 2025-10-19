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

    [Header("Camera")]
    public Camera mainCamera; // Drag your main camera here
    public float cameraZ = -15f; // Default Z for 2D camera
    public float cameraGap = 10f; // Gap around the grid


    void Start()
    {
        GenerateGrid();
        CenterCamera();

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
        CenterCamera();
    }

    // Call this after generating the grid
    void CenterCamera()
    {
       if (mainCamera == null) return;

        // Calculate grid center
        float centerX = (columns - 1) * cellSize / 2f;
        float centerY = (rows - 1) * cellSize / 2f;

        // Move camera to center
        mainCamera.transform.position = new Vector3(centerX, centerY, cameraZ);

        // Calculate orthographic size to fit the grid + gap
        float gridWidth = columns * cellSize + cameraGap * 2f;
        float gridHeight = rows * cellSize + cameraGap * 2f;

        // Orthographic size is half of vertical height
        float sizeY = gridHeight / 2f;

        // Horizontal size based on aspect ratio
        float sizeX = (gridWidth / 2f) / mainCamera.aspect;

        // Choose the larger size so entire grid + gap fits
        mainCamera.orthographicSize = Mathf.Max(sizeX, sizeY);
    }

}
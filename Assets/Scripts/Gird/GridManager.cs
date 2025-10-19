using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 8;
    public int rows = 8;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("Tile Selection")]
    public static Color selectedColor = Color.white; // default
    public Button whiteButton;
    public Button greenButton;
    public Button redButton;

    void Start()
    {
        GenerateGrid();
        SetupButtons();

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

        void SetupButtons()
    {
        whiteButton.onClick.AddListener(() => SelectColor(Color.white));
        greenButton.onClick.AddListener(() => SelectColor(Color.green));
        redButton.onClick.AddListener(() => SelectColor(Color.red));
    }

    void SelectColor(Color color)
    {
        selectedColor = color;
    }
}

using UnityEngine;

public class Cell : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        ApplySelectedTile();
    }

    void ApplySelectedTile()
    {
        spriteRenderer.color = GridManager.selectedColor;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 /// <summary>
 /// Represents a single tile in the grid. It can be initialized with different colors 
 /// based on its position (offset or base) and provides visual feedback on mouse hover.
 /// </summary>
 
[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour {
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color offsetColor = Color.gray;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject highlight;

    void OnValidate()
    {
        // Auto-assign in editor if not set
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevent accidental fully-transparent colors from the inspector
        if (baseColor.a <= 0f) baseColor.a = 1f;
        if (offsetColor.a <= 0f) offsetColor.a = 1f;
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Hide highlight by default (safe)
        if (highlight != null)
            highlight.SetActive(false);
    }
 
    public void Init(bool isOffset) {
        if (spriteRenderer == null) {
            Debug.LogWarning($"Tile ({name}) has no SpriteRenderer assigned.");
            return;
        }

        // Choose color and ensure visible alpha
        Color c = isOffset ? offsetColor : baseColor;
        if (c.a <= 0f) c.a = 1f;
        spriteRenderer.color = c;
    }
 
    void OnMouseEnter() {
        if (highlight != null) highlight.SetActive(true);
    }
 
    void OnMouseExit()
    {
        if (highlight != null) highlight.SetActive(false);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 /// <summary>
 /// Represents a single tile in the grid. It can be initialized with different colors 
 /// based on its position (offset or base) and provides visual feedback on mouse hover.
 /// </summary>
 
[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour {
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _offsetColor = Color.gray;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private GameObject _highlight;

    void OnValidate()
    {
        // Auto-assign in editor if not set
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        // Prevent accidental fully-transparent colors from the inspector
        if (_baseColor.a <= 0f) _baseColor.a = 1f;
        if (_offsetColor.a <= 0f) _offsetColor.a = 1f;
    }

    void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        // Hide highlight by default (safe)
        if (_highlight != null)
            _highlight.SetActive(false);
    }
 
    public void Init(bool isOffset) {
        if (_renderer == null) {
            Debug.LogWarning($"Tile ({name}) has no SpriteRenderer assigned.");
            return;
        }

        // Choose color and ensure visible alpha
        Color c = isOffset ? _offsetColor : _baseColor;
        if (c.a <= 0f) c.a = 1f;
        _renderer.color = c;
    }
 
    void OnMouseEnter() {
        if (_highlight != null) _highlight.SetActive(true);
    }
 
    void OnMouseExit()
    {
        if (_highlight != null) _highlight.SetActive(false);
    }
}
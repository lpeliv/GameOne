using UnityEngine;

public class BuildableTile : MonoBehaviour
{
    private bool isOccupied;
    public bool IsOccupied => isOccupied;

    private Renderer tileRenderer;
    private Color originalColor;

    [Header("Highlight Colors")]
    [SerializeField] private Color canBuildColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color cannotBuildColor = new Color(1f, 0f, 0f, 0.5f);

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = tileRenderer.material.color;
    }

    public void Highlight(bool canBuild)
    {
        tileRenderer.material.color = canBuild ? canBuildColor : cannotBuildColor;
    }

    public void ClearHighlight()
    {
        tileRenderer.material.color = originalColor;
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
}
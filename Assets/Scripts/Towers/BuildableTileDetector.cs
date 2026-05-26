using UnityEngine;

public class BuildableTileDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private LayerMask tileLayer;

    [Header("References")]
    [SerializeField] private TowerManager towerManager;
    [SerializeField] private BuildPromptUI buildPromptUI;

    [Header("Build Mode")]
    [SerializeField] private KeyCode buildModeKey = KeyCode.B;

    private bool buildModeActive = false;
    private BuildableTile currentDetectedTile;

    public bool BuildModeActive => buildModeActive;

    private void Update()
    {
        if (Input.GetKeyDown(buildModeKey))
            ToggleBuildMode();

        if (buildModeActive)
            DetectTile();
        else
            ClearCurrentTile();
    }

    private void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;
        Debug.Log($"[BuildableTileDetector] Build mode: {buildModeActive}");

        if (!buildModeActive)
        {
            ClearCurrentTile();
            buildPromptUI.Hide();
        }
    }

    private void DetectTile()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, detectionRange, tileLayer))
        {
            BuildableTile tile = hit.collider.GetComponent<BuildableTile>();

            if (tile != null && tile != currentDetectedTile)
            {
                ClearCurrentTile();
                currentDetectedTile = tile;
                bool canBuild = towerManager.CanBuildAt(tile);
                currentDetectedTile.Highlight(canBuild);
                towerManager.RequestBuild(tile);
                buildPromptUI.Show(tile, canBuild);
            }
        }
        else
        {
            ClearCurrentTile();
        }
    }

    private void ClearCurrentTile()
    {
        if (currentDetectedTile == null) return;
        currentDetectedTile.ClearHighlight();
        buildPromptUI.Hide();
        currentDetectedTile = null;
    }
}
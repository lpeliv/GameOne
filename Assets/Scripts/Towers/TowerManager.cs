using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private BuildPromptUI buildPromptUI;

    [Header("Turret")]
    [SerializeField] private GameObject turretBasePrefab;

    [SerializeField] private BuildableTileDetector tileDetector;

    private List<BuildableTile> occupiedTiles = new List<BuildableTile>();
    private BuildableTile pendingTile;

    public bool CanBuildAt(BuildableTile tile)
    {
        if (tile == null) return false;
        if (tile.IsOccupied) return false;
        if (waveManager.WaveActive) return false;
        if (!tileDetector.BuildModeActive) return false;
        return true;
    }

    public void RequestBuild(BuildableTile tile)
    {
        if (!CanBuildAt(tile)) return;

        pendingTile = tile;
        buildPromptUI.SetOnConfirm(ConfirmBuild);
    }

    private void ConfirmBuild()
    {
        if (pendingTile == null) return;
        if (!CanBuildAt(pendingTile))
        {
            pendingTile = null;
            return;
        }

        PlaceTurretBase(pendingTile);
        pendingTile = null;
    }

    private void PlaceTurretBase(BuildableTile tile)
    {
        if (turretBasePrefab == null)
        {
            Debug.LogWarning("[TowerManager] No turret base prefab assigned.");
            return;
        }

        Vector3 spawnPos = new Vector3(
            tile.transform.position.x,
            tile.transform.position.y,
            tile.transform.position.z
        );

        GameObject turret = Instantiate(turretBasePrefab, spawnPos, Quaternion.identity);
        tile.SetOccupied(true);
        occupiedTiles.Add(tile);

        Debug.Log($"[TowerManager] Turret base placed at {spawnPos}.");
    }

    public void RemoveTurret(BuildableTile tile)
    {
        if (!occupiedTiles.Contains(tile)) return;
        tile.SetOccupied(false);
        occupiedTiles.Remove(tile);
    }
}
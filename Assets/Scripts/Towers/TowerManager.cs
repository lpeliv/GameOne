using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private BuildPromptUI buildPromptUI;

    [Header("Turret")]
    [SerializeField] private TurretDefinition turretDefinition;

    [SerializeField] private BuildableTileDetector tileDetector;
    
    [Header("Testing")]
    [SerializeField] private AddonDefinition testAddonDefinition;
    [SerializeField] private GameObject testAddonPrefab;

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
        Debug.Log("[TowerManager] PlaceTurretBase called.");

        if (turretDefinition?.basePrefab == null)
        {
            Debug.LogWarning("[TowerManager] No turret base prefab assigned.");
            return;
        }

        Vector3 spawnPos = tile.transform.position;
        GameObject turretGO = Instantiate(turretDefinition.basePrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[TowerManager] Instantiated: {turretGO != null}, Name: {turretGO?.name}");
        TurretBase turretBase = turretGO.GetComponent<TurretBase>();
        Debug.Log($"[TowerManager] TurretBase component found: {turretBase != null}");

        if (turretBase == null)
        {
            Debug.LogWarning("[TowerManager] Turret base prefab missing TurretBase component.");
            Destroy(turretGO);
            return;
        }

        turretBase.Initialize(turretDefinition);
        tile.SetOccupied(true);
        occupiedTiles.Add(tile);

        Debug.Log($"[TowerManager] Turret base placed at {spawnPos}.");

        // Disabled test addon (addon definition)
        //TurretCylinder firstCylinder = turretBase.GetCylinder(0);
        //if (firstCylinder != null && testAddonDefinition != null && testAddonPrefab != null)
        //{
        //    GameObject addonGO = Instantiate(testAddonPrefab);
        //    TurretAddon addon = addonGO.GetComponent<TurretAddon>();

        //    if (addon != null)
        //    {
        //        addon.Initialize(testAddonDefinition);
        //        firstCylinder.Joint.Attach(addon);
        //    }
        //}
    }

    public void RemoveTurret(BuildableTile tile)
    {
        if (!occupiedTiles.Contains(tile)) return;
        tile.SetOccupied(false);
        occupiedTiles.Remove(tile);
    }
}
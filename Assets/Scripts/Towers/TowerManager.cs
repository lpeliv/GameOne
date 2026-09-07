using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;

    private List<BuildableTile> occupiedTiles = new List<BuildableTile>();

    public bool CanBuildAt(BuildableTile tile)
    {
        if (tile == null) return false;
        if (tile.IsOccupied) return false;
        if (waveManager.WaveActive) return false;
        return true;
    }

    public void RequestBuild(BuildableTile tile, TurretDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[TowerManager] RequestBuild called with null TurretDefinition.");
            return;
        }
        if (!PlayerInventory.Instance.HasBase(def))
        {
            Debug.LogWarning("[TowerManager] No base available in inventory.");
            return;
        }
        if (!CanBuildAt(tile)) return;

        PlaceTurretBase(tile, def);
        PlayerInventory.Instance.RemoveBase(def);
    }

    private void PlaceTurretBase(BuildableTile tile, TurretDefinition def)
    {
        if (def.basePrefab == null)
        {
            Debug.LogWarning("[TowerManager] No turret base prefab assigned.");
            return;
        }

        Vector3 spawnPos = tile.transform.position;
        GameObject turretGO = Instantiate(def.basePrefab, spawnPos, Quaternion.identity);
        TurretBase turretBase = turretGO.GetComponent<TurretBase>();

        if (turretBase == null)
        {
            Debug.LogWarning("[TowerManager] Turret base prefab missing TurretBase component.");
            Destroy(turretGO);
            return;
        }

        turretBase.Initialize(def);
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

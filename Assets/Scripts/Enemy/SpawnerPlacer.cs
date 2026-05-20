using System.Collections.Generic;
using UnityEngine;

public class SpawnerPlacer
{
    private readonly Grid gridInstance;
    private readonly Vector2Int worldOffset;
    private readonly Side zone;

    public List<SpawnerData> placedSpawners;

    public SpawnerPlacer(Grid gridInstance, Vector2Int worldOffset, Side zone)
    {
        this.gridInstance = gridInstance;
        this.worldOffset = worldOffset;
        this.zone = zone;
    }

    public void Place(Vector2Int startingPos, List<Vector2Int> branchSpawnPoints)
    {
        placedSpawners = new List<SpawnerData>();

        Register(startingPos, SpawnerType.Main);

        if (branchSpawnPoints != null)
            foreach (Vector2Int bp in branchSpawnPoints)
                Register(bp, SpawnerType.Branch);
    }

    private void Register(Vector2Int localPos, SpawnerType type)
    {
        Vector3 worldPos = new Vector3(
            (worldOffset.x + localPos.x + 0.5f) * MasterManager.TileScale,
            1f,
            (worldOffset.y + localPos.y + 0.5f) * MasterManager.TileScale

        );

        placedSpawners.Add(new SpawnerData(localPos, worldPos, zone, type));
    }
}

using System.Collections.Generic;
using UnityEngine;

public class ObstaclePlacer
{
    private Grid gridInstance;
    private ObstacleCatalogue catalogue;
    private Vector2Int worldOffset;

    public List<ObstaclePlacementData> placedObstacles;

    public ObstaclePlacer(Grid gridInstance, ObstacleCatalogue catalogue, Vector2Int worldOffset)
    {
        this.gridInstance = gridInstance;
        this.catalogue = catalogue;
        this.worldOffset = worldOffset;
    }

    public void Place(float density, int minSpacing)
    {
        placedObstacles = new List<ObstaclePlacementData>();

        List<Vector2Int> candidates = CollectCandidates();

        int targetCount = Mathf.RoundToInt(candidates.Count * density);

        ShuffleList(candidates);

        int placed = 0;
        foreach(Vector2Int candidate in candidates)
        {
            if (placed >= targetCount) break;

            ObstacleDefinition def = catalogue.GetWeightedRandom();

            bool swapOrientation = Random.value > 0.5f && def.sizeX != def.sizeZ;
            int sizeX = swapOrientation ? def.sizeZ : def.sizeX;
            int sizeZ = swapOrientation ? def.sizeX : def.sizeZ;

            if(TryPlace(candidate, def, sizeX, sizeZ, minSpacing))
                placed++;
        }

    }

    private List<Vector2Int> CollectCandidates()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for(int x = 0; x < gridInstance.gridWidth; x++)
        {
            for(int z = 0;  z < gridInstance.gridHeight; z++)
            {
                if (gridInstance.gridArray[x, z].type == TileType.Obstacle)
                    candidates.Add(new Vector2Int(x, z));
            }
        }

        return candidates;
    }

    private bool TryPlace(Vector2Int root, ObstacleDefinition def, int sizeX,  int sizeZ, int minSpacing)
    {
        List<Vector2Int> footprint = GetFootprint(root, sizeX, sizeZ);
        foreach(Vector2Int tile in footprint)
        {
            if(!IsInsideGrid(tile)) return false;

            if (gridInstance.gridArray[tile.x, tile.y].type != TileType.Obstacle)
                return false;
        }

        Vector2Int centre = new Vector2Int(
            root.x + sizeX / 2,
            root.y + sizeZ / 2
        );

        foreach(ObstaclePlacementData existing in placedObstacles)
        {
            Vector2Int existingCentre = new Vector2Int(
                existing.rootTile.x + existing.sizeX / 2,
                existing.rootTile.y + existing.sizeZ / 2
            );

            if(ManhattanDistance(centre, existingCentre) < minSpacing)
                return false;
        }

        foreach (Vector2Int tile in footprint)
            SetTile(tile, TileType.OccupiedObstacle);

        placedObstacles.Add(new ObstaclePlacementData(
            def.displayName,
            root,
            sizeX,
            sizeZ,
            footprint
        ));

        return true;
    }

    private List<Vector2Int> GetFootprint(Vector2Int root, int sizeX, int sizeZ)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        for(int dx = 0; dx < sizeX; dx++)
            for(int dz = 0; dz < sizeZ; dz++)
                tiles.Add(new Vector2Int(root.x + dx, root.y + dz));
        return tiles;
    }

    private bool IsInsideGrid(Vector2Int pos) =>
        pos.x >= 0 && pos.y >= 0 &&
        pos.x < gridInstance.gridWidth &&
        pos.y < gridInstance.gridHeight;

    private int ManhattanDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private void SetTile(Vector2Int pos, TileType type)
    {
        Tile tile = gridInstance.gridArray[pos.x, pos.y];
        tile.type = type;
        gridInstance.gridArray[pos.x, pos.y] = tile;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for(int i = list.Count - 1; i >= 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

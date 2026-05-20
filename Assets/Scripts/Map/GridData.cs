using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridData
{
    public int width;
    public int height;
    public Vector2Int startpos;
    public Vector2Int endpos;
    public List<TileData> tiles;
    public List<Vector2Int> spawnpoints;
}

[System.Serializable]
public class TileData
{
    public int x;
    public int z;
    public TileType tileType;
}

[System.Serializable]
public class FullMapData
{
    public GridData targetData;
    public ZoneGridData topData;
    public ZoneGridData bottomData;
    public ZoneGridData leftData;
    public ZoneGridData rightData;
}

[System.Serializable]
public class ZoneGridData
{
    public int width;
    public int height;
    public Vector2Int worldOffset;
    public Side facingSide;
    public Vector2Int endingPos;
    public Vector2Int startingPos;
    public List<TileData> tiles;
    public List<Vector2Int> spawnpoints;
    public List<Vector2Int> branchSpawnPoints;
    public List<Vector2Int> branchMergeTiles;
    public List<ObstaclePlacementData> obstacles;
    public List<SpawnerData> spawners;
}
using UnityEngine;

public enum SpawnerType
{
    Main,
    Branch,
}

[System.Serializable]
public class SpawnerData
{
    public Vector2Int gridPos;
    public Vector3 worldPos;
    public Side zone;
    public SpawnerType spawnerType;

    public SpawnerData(Vector2Int gridPos, Vector3 worldPos, Side zone, SpawnerType spawnerType)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
        this.zone = zone;
        this.spawnerType = spawnerType;
    }
}

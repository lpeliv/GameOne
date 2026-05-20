using System.Collections.Generic;
using UnityEngine;

public class EnemyPathBuilder
{
    private readonly Grid gridInstance;
    private readonly Vector2Int worldOffset;
    private readonly Side zone;

    public EnemyPathBuilder(Grid gridInstance, Vector2Int worldOffset, Side zone)
    {
        this.gridInstance = gridInstance;
        this.worldOffset = worldOffset;
        this.zone = zone;
    }

    public EnemyPath Build(Vector2Int startingPos, Vector2Int endingPos)
    {
        List<Vector3> waypoints = new List<Vector3>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int current = startingPos;
        visited.Add(current);
        waypoints.Add(ToWorldPos(current));

        while (current != endingPos)
        {
            Vector2Int? next = GetNextTile(current, visited, endingPos);

            if (next == null)
            {
                break;
            }

            current = next.Value;
            visited.Add(current);
            waypoints.Add(ToWorldPos(current));
        }

        return new EnemyPath(waypoints, zone);
    }

    private Vector2Int? GetNextTile(Vector2Int current, HashSet<Vector2Int> visited, Vector2Int endingPos)
    {
        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int neighbour = current + dir;

            if (!IsInsideGrid(neighbour)) continue;
            if (visited.Contains(neighbour)) continue;

            TileType type = gridInstance.gridArray[neighbour.x, neighbour.y].type;

            if (type == TileType.Path || type == TileType.Spawner)
                return neighbour;
        }

        return null;
    }

    private Vector3 ToWorldPos(Vector2Int gridPos) => new Vector3(
        (worldOffset.x + gridPos.x + 0.5f) * MasterManager.TileScale,
        1f,
        (worldOffset.y + gridPos.y + 0.5f) * MasterManager.TileScale
    );

    private bool IsInsideGrid(Vector2Int pos) =>
        pos.x >= 0 && pos.y >= 0 &&
        pos.x < gridInstance.gridWidth &&
        pos.y < gridInstance.gridHeight;

    private static readonly Vector2Int[] cardinals =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };
}
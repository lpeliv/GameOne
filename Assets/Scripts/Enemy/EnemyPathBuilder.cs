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

    public EnemyPath Build(List<Vector2Int> mainPathTiles)
    {
        List<Vector3> waypoints = new List<Vector3>();

        foreach (Vector2Int tile in mainPathTiles)
            waypoints.Add(ToWorldPos(tile));

        return new EnemyPath(waypoints, zone);
    }

    private Vector2Int? GetNextTile(Vector2Int current, HashSet<Vector2Int> visited, Vector2Int endingPos)
    {
        Vector2Int? best = null;
        int bestDist = int.MaxValue;

        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int neighbour = current + dir;

            if (!IsInsideGrid(neighbour)) continue;
            if (visited.Contains(neighbour)) continue;

            TileType type = gridInstance.gridArray[neighbour.x, neighbour.y].type;
            if (type != TileType.Path && type != TileType.Spawner) continue;

            int dist = Mathf.Abs(neighbour.x - endingPos.x) +
                       Mathf.Abs(neighbour.y - endingPos.y);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = neighbour;
            }
        }

        return best;
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
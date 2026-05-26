using System.Collections.Generic;
using UnityEngine;

public class ZonePathGenerator
{
    private Grid gridInstance;
    private Vector2Int endingPos;
    private Vector2Int startingPos;
    private Side facingSide;
    private int maxAttempts = 1000;
    Vector2Int worldOffset;
    HashSet<Vector2Int> globalWalls;

    public List<Vector2Int> spawnPoints;


    public List<Vector2Int> branchSpawnPoints;
    public List<Vector2Int> edgeCandidates;

    public List<Vector2Int> builtPath = new List<Vector2Int>();

    public ZonePathGenerator(Grid grid, Vector2Int ending, Vector2Int starting, Side facingSide, Vector2Int worldOffset, HashSet<Vector2Int> globalWalls)
    {
        this.gridInstance = grid;
        this.endingPos = ending;
        this.startingPos = starting;
        this.facingSide = facingSide;
        this.worldOffset = worldOffset;
        this.globalWalls = globalWalls;
    }

    public void Generate()
    {
        for(int i = 0; i < maxAttempts;  i++)
        {
            ResetPaths();
            if (RunGeneration())
            {
                break;
            }
        }
    }

    private void ResetPaths()
    {
        for (int x = 0; x < gridInstance.gridWidth; x++)
        {
            for(int z = 0;  z < gridInstance.gridHeight; z++)
            {
                Tile tile = gridInstance.gridArray[x, z];
                if(tile.type == TileType.Path)
                {
                    tile.type = TileType.Obstacle;
                    gridInstance.gridArray[x, z] = tile;
                }
            }
        }
    }

    private bool RunGeneration()
    {
        builtPath.Clear();
        Vector2Int currentPos = startingPos;

        Tile startTile = gridInstance.gridArray[currentPos.x, currentPos.y];
        startTile.type = TileType.Path;
        gridInstance.gridArray[currentPos.x, currentPos.y] = startTile;
        builtPath.Add(currentPos);

        int stepLimit = gridInstance.gridWidth * gridInstance.gridHeight * 10;

        for (int step = 0; step < stepLimit; step++)
        {
            Vector2Int direction = GetRandomDirection(currentPos);

            if (direction == Vector2Int.zero)
                return false;

            Vector2Int nextPos = currentPos + direction;

            if (nextPos == endingPos)
            {
                Tile endTile = gridInstance.gridArray[nextPos.x, nextPos.y];
                endTile.type = TileType.Path;
                gridInstance.gridArray[nextPos.x, nextPos.y] = endTile;
                builtPath.Add(nextPos);
                return true;
            }

            if (!IsInsideGrid(nextPos) || IsWall(nextPos))
                return false;

            Tile tile = gridInstance.gridArray[nextPos.x, nextPos.y];
            tile.type = TileType.Path;
            gridInstance.gridArray[nextPos.x, nextPos.y] = tile;
            builtPath.Add(nextPos);

            currentPos = nextPos;
        }
        return false;
    }

    private Vector2Int GetRandomDirection(Vector2Int currentPos)
    {
        List<Vector2Int> valid = new List<Vector2Int>();
        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int next = currentPos + dir;
            if (IsWalkable(next))
                valid.Add(dir);
        }

        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : Vector2Int.zero;
    }

    public bool IsInsideGrid(Vector2Int pos) =>
        pos.x >= 0 && pos.y >= 0 &&
        pos.x < gridInstance.gridWidth &&
        pos.y < gridInstance.gridHeight;

    private bool IsWall(Vector2Int pos) =>
        gridInstance.gridArray[pos.x, pos.y].type == TileType.Wall;

    private bool IsWalkable(Vector2Int pos) =>
        IsInsideGrid(pos) &&
        gridInstance.gridArray[pos.x, pos.y].type != TileType.Wall &&
        gridInstance.gridArray[pos.x, pos.y].type != TileType.Path &&
        GetPathNeighbourCount(pos) < 2 &&
        !HasWallNeighbour(pos);

    private int GetPathNeighbourCount(Vector2Int pos)
    {
        int count = 0;
        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;
            if (IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Path)
                count++;
        }
        return count;
    }

    private bool HasWallNeighbour(Vector2Int pos)
    {
        if (pos == endingPos)
            return false;

        foreach(Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;
            if(IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Wall)
                return true;

            Vector2Int worldPos = n + worldOffset;
            if(globalWalls.Contains(worldPos))
                return true;
        }
        return false;
    }

    private static readonly Vector2Int[] cardinals = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    public void GenerateBuildables(List <Vector2Int> allSpawners)
    {
        for(int x = 0; x < gridInstance.gridWidth; x++)
        {
            for(int z = 0;  z < gridInstance.gridHeight; z++)
            {
                if (gridInstance.gridArray[x, z].type != TileType.Path)
                    continue;

                for(int dx = -1; dx <= 1; dx++)
                {
                    for(int dz = -1;  dz <= 1; dz++)
                    {
                        if(dx == 0 && dz == 0) continue;

                        Vector2Int neighbour = new Vector2Int(x + dx, z + dz);

                        if (!IsInsideGrid(neighbour)) continue;

                        if (gridInstance.gridArray[neighbour.x, neighbour.y].type != TileType.Obstacle)
                            continue;

                        SetTile(neighbour, TileType.Buildable);
                    }
                }
            }
        }

        for(int dx = -1; dx <= 1; dx++)
        {
            for(int dz = -1; dz <= 1; dz++)
            {
                Vector2Int pos = new Vector2Int(endingPos.x + dx, endingPos.y + dz);

                if(!IsInsideGrid(pos)) continue;

                if (gridInstance.gridArray[pos.x, pos.y].type != TileType.Buildable)
                    continue;

                SetTile(pos, TileType.Obstacle);
            }
        }

        foreach (Vector2Int spawner  in allSpawners)
        {
            for(int dx = -2 ; dx <= 2; dx++)
            {
                for( int dz = -2 ; dz <= 2; dz++)
                {
                    Vector2Int pos = new Vector2Int(spawner.x + dx, spawner.y + dz);

                    if(!IsInsideGrid(pos)) continue;

                    if (gridInstance.gridArray[pos.x, pos.y].type != TileType.Buildable)
                        continue;

                    SetTile(pos, TileType.Obstacle);
                }
            }
        }
    }

    private void SetTile(Vector2Int pos, TileType type)
    {
        Tile tile = gridInstance.gridArray[pos.x, pos.y];
        tile.type = type;
        gridInstance.gridArray[pos.x, pos.y] = tile;
    }
}
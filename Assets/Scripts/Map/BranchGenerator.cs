using System.Collections.Generic;
using UnityEngine;

public class BranchGenerator
{
    private Grid gridInstance;
    private Vector2Int startingPos;
    private Vector2Int endingPos;
    private Side facingSide;
    private Vector2Int worldOffset;
    private HashSet<Vector2Int> globalWalls;

    public List<Vector2Int> branchSpawnPoints;
    public List<Vector2Int> edgeCandidates;
    private int maxAttempts = 1000;

    public List<Vector2Int> branchMergeTiles;

    public BranchGenerator(Grid grid, Vector2Int startingPos, Vector2Int endingPos, Side facingSide, Vector2Int worldOffset, HashSet<Vector2Int> globalWalls)
    {
        this.gridInstance = grid;
        this.startingPos = startingPos;
        this.endingPos = endingPos;
        this.facingSide = facingSide;
        this.worldOffset = worldOffset;
        this.globalWalls = globalWalls;
    }

    public void GenerateBranchSpawners(int count, int minSpacing, int endpointExclusionRadius)
    {
        edgeCandidates = CollectEdgeCandidate(endpointExclusionRadius);
        branchSpawnPoints = SelectSpawnPoints(edgeCandidates, count, minSpacing);
        branchMergeTiles = new List<Vector2Int>();
    }

    private List<Vector2Int> CollectEdgeCandidate(int endpointExclusionRadius)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int w = gridInstance.gridWidth;
        int h = gridInstance.gridHeight;

        switch (facingSide)
        {
            case Side.Bottom:
                for (int x = 1; x < w - 1; x++)
                    TryAddCandidate(candidates, new Vector2Int(x, h - 2), endpointExclusionRadius);
                for (int z = 1; z < h - 2; z++)
                {
                    TryAddCandidate(candidates, new Vector2Int(1, z), endpointExclusionRadius);
                    TryAddCandidate(candidates, new Vector2Int(w - 2, z), endpointExclusionRadius);
                }
                break;
            case Side.Top:
                for (int x = 1; x < w - 1; x++)
                    TryAddCandidate(candidates, new Vector2Int(x, 1), endpointExclusionRadius);
                for (int z = 1; z < h - 2; z++)
                {
                    TryAddCandidate(candidates, new Vector2Int(1, z), endpointExclusionRadius);
                    TryAddCandidate(candidates, new Vector2Int(w - 2, z), endpointExclusionRadius);
                }
                break;
            case Side.Left:
                for (int z = 1; z < h - 1; z++)
                    TryAddCandidate(candidates, new Vector2Int(w - 2, z), endpointExclusionRadius);
                for (int x = 1; x < w - 2; x++)
                {
                    TryAddCandidate(candidates, new Vector2Int(x, 1), endpointExclusionRadius);
                    TryAddCandidate(candidates, new Vector2Int(x, h - 2), endpointExclusionRadius);
                }
                break;
            case Side.Right:
                for (int z = 1; z < h - 1; z++)
                    TryAddCandidate(candidates, new Vector2Int(1, z), endpointExclusionRadius);
                for (int x = 1; x < w - 2; x++)
                {
                    TryAddCandidate(candidates, new Vector2Int(x, 1), endpointExclusionRadius);
                    TryAddCandidate(candidates, new Vector2Int(x, h - 2), endpointExclusionRadius);
                }
                break;
        }
        return candidates;
    }

    private void TryAddCandidate(List<Vector2Int> candidates, Vector2Int pos, int exclusionRadius)
    {
        if (ManhattanDistance(pos, endingPos) < exclusionRadius)
            return;
        if (pos == startingPos)
            return;
        if (IsCorner(pos))
            return;
        if (gridInstance.gridArray[pos.x, pos.y].type != TileType.Obstacle)
            return;
        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;
            if (IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Path)
            {
                return;
            }
        }
        candidates.Add(pos);
    }

    private static readonly Vector2Int[] cardinals = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private List<Vector2Int> SelectSpawnPoints(List<Vector2Int> candidates, int count, int minSpacing)
    {
        ShuffleList(candidates);

        List<Vector2Int> selected = new List<Vector2Int>();

        foreach (Vector2Int candidate in candidates)
        {
            if (selected.Count >= count)
                break;

            if (IsTooClose(candidate, selected, minSpacing))
                continue;

            selected.Add(candidate);
        }

        return selected;
    }

    private bool IsTooClose(Vector2Int candidate, List<Vector2Int> selected, int minSpaced)
    {
        foreach (Vector2Int s in selected)
            if (ManhattanDistance(candidate, s) < minSpaced)
                return true;

        if (ManhattanDistance(candidate, startingPos) < minSpaced)
            return true;

        return false;
    }

    private bool IsCorner(Vector2Int pos)
    {
        bool wallX = IsWallAt(pos + Vector2Int.left) || IsWallAt(pos + Vector2Int.right);
        bool wallY = IsWallAt(pos + new Vector2Int(0, 1)) || IsWallAt(pos + new Vector2Int(0, -1));
        return wallX && wallY;
    }

    private bool IsWallAt(Vector2Int pos)
    {
        if(!IsInsideGrid(pos)) return false;
        return gridInstance.gridArray[pos.x, pos.y].type == TileType.Wall;
    }

    public bool IsInsideGrid(Vector2Int pos) =>
        pos.x >= 0 && pos.y >= 0 &&
        pos.x < gridInstance.gridWidth &&
        pos.y < gridInstance.gridHeight;

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    public void GenerateBranches()
    {
        if(branchSpawnPoints == null || branchSpawnPoints.Count == 0) return;

        foreach (Vector2Int spawner in branchSpawnPoints)
            TryGenerateBranch(spawner);
        
    }

    private bool TryGenerateBranch(Vector2Int spawner)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            ResetBranch();
            Vector2Int mergeTitle = Vector2Int.zero;
            if (RunBranchGeneration(spawner, out mergeTitle))
            {
                branchMergeTiles.Add(mergeTitle);
                return true;
            }
        }
        return false;
    }

    private bool RunBranchGeneration(Vector2Int spawner, out Vector2Int mergeTile)
    {
        mergeTile = Vector2Int.zero;
        Vector2Int currentPos = spawner;
        SetTile(currentPos, TileType.Branch);

        int stepLimit = gridInstance.gridWidth * gridInstance.gridHeight * 10;

        for (int step = 0; step < stepLimit; step++)
        {
            if (IsAdjacentToPath(currentPos))
            {
                mergeTile = currentPos;
                CommitBranch();
                return true;
            }

            Vector2Int direction = GetBranchDirection(currentPos);

            if (direction == Vector2Int.zero)
            {
                ResetBranch();
                return false;
            }

            Vector2Int nextPos = currentPos + direction;

            if (gridInstance.gridArray[nextPos.x, nextPos.y].type == TileType.Path)
            {
                mergeTile = currentPos;
                CommitBranch();
                return true;
            }

            SetTile(nextPos, TileType.Branch);
            currentPos = nextPos;
        }

        ResetBranch();
        return false;
    }

    private bool IsAdjacentToPath(Vector2Int pos)
    {
        foreach(Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;

            if(IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Path)
                    return true;
        }

        return false;
    }

    private Vector2Int GetBranchDirection(Vector2Int currentPos)
    {
        List<Vector2Int> valid = new List<Vector2Int>();
        foreach(Vector2Int dir in cardinals)
        {
            Vector2Int next = currentPos + dir;
            if(IsBranchWalkable(next))
                valid.Add(dir);
        }

        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : Vector2Int.zero;
    }

    private bool IsBranchWalkable(Vector2Int pos)
    {
        if(!IsInsideGrid(pos)) return false;

        TileType type = gridInstance.gridArray[pos.x, pos.y].type;

        if(type != TileType.Obstacle) return false;

        if(GetBranchNeighbour(pos) >= 2) return false;
        if(HasWallNeighbour(pos)) return false;

        return true;
    }

    private int GetBranchNeighbour(Vector2Int pos)
    {
        int count = 0;
        foreach (Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;
            if(IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Branch)
                count++;
        }
        return count;
    }

    private void CommitBranch()
    {
        for (int x = 0; x < gridInstance.gridWidth; x++)
            for (int z = 0; z < gridInstance.gridHeight; z++)
                if (gridInstance.gridArray[x, z].type == TileType.Branch)
                    SetTile(new Vector2Int(x, z), TileType.Path);
    }

    private void ResetBranch()
    {
        for (int x = 0; x < gridInstance.gridWidth; x++)
            for (int z = 0; z < gridInstance.gridHeight; z++)
                if (gridInstance.gridArray[x, z].type == TileType.Branch)
                    SetTile(new Vector2Int(x, z), TileType.Obstacle);
    }

    private void SetTile(Vector2Int pos, TileType type)
    {
        Tile tile = gridInstance.gridArray[pos.x, pos.y];
        tile.type = type;
        gridInstance.gridArray[pos.x, pos.y] = tile;
    }

    private bool HasWallNeighbour(Vector2Int pos)
    {
        foreach(Vector2Int dir in cardinals)
        {
            Vector2Int n = pos + dir;
            if (IsInsideGrid(n) && gridInstance.gridArray[n.x, n.y].type == TileType.Wall)
                return true;

            Vector2Int worldPos = n + worldOffset;
            if (globalWalls.Contains(worldPos))
                return true;
        }
        return false;
    }
}
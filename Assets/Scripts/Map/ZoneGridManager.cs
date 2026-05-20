using System.Collections.Generic;
using UnityEngine;

public class ZoneGridManager : MonoBehaviour
{
    [SerializeField] public MapVisualizer visualizer;
    private Grid gridInstance;
    public Vector2Int worldOffset;
    private Side facingSide;
    private Vector2Int endingPos;
    private Vector2Int targetSize;
    private Vector2Int targetOffset;
    public List<Vector2Int> wallTiles;
    public ZonePathGenerator pathGenerator;
    public BranchGenerator branchGenerator;
    private Vector2Int startingPos;
    private HashSet<Vector2Int> globalWalls;
    
    public List<Vector2Int> WallTiles => wallTiles;
    public Vector2Int WorldOffset => worldOffset;
    public Grid Grid => gridInstance;

    [SerializeField] private float obstacleDensity = 0.4f;
    [SerializeField] private int obstacleMinSpacing = 3;
    [SerializeField] public ObstacleCatalogue obstacleCatalogue;

    //[Header("Spawner Settings")]
    //[SerializeField] private int spawnerMinDistanceFromEnd = 6;

    public SpawnerPlacer spawnerPlacer;
    public ObstaclePlacer obstaclePlacer;
    public EnemyPath enemyPath;

    public void Initialize(int width, int height, Vector2Int offset, Side facing, Vector2Int targetSize, Vector2Int targetOffset, Vector2Int worldOffset, HashSet<Vector2Int> globalWalls)
    {
        this.targetSize = targetSize;
        this.targetOffset = targetOffset;
        this.worldOffset = worldOffset;
        this.globalWalls = globalWalls;
        facingSide = facing;
        gridInstance = new Grid(width, height);
        wallTiles = new List<Vector2Int>();

        GenerateOuterWall();
        GenerateWall();

        SetEndingPos();
        SetStartingPos();
    }

    private void OnDrawGizmos()
    {
        if(gridInstance == null) return;

        Gizmos.color = Color.white;
        for (int x = 0; x <= gridInstance.gridWidth; x++)
        {
            Gizmos.DrawLine(new Vector3((x + worldOffset.x) * MasterManager.TileScale, 0, worldOffset.y * MasterManager.TileScale), 
                new Vector3((x + worldOffset.x) * MasterManager.TileScale, 0, (worldOffset.y + gridInstance.gridHeight) * MasterManager.TileScale));
        }
        for (int z = 0; z <= gridInstance.gridHeight; z++)
        {
            Gizmos.DrawLine(new Vector3(worldOffset.x * MasterManager.TileScale, 0, (z + worldOffset.y) * MasterManager.TileScale),
                new Vector3((worldOffset.x + gridInstance.gridWidth) * MasterManager.TileScale, 0, (z + worldOffset.y) * MasterManager.TileScale));
        }

        for (int x = 0; x < gridInstance.gridWidth; x++)
        {
            for (int z = 0; z < gridInstance.gridHeight; z++)
            {
                Vector3 center = new Vector3((worldOffset.x + x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + z + 0.5f) * MasterManager.TileScale);
                Tile tile = gridInstance.gridArray[x, z];

                Gizmos.color = tile.type switch
                {
                    TileType.Wall => Color.black,
                    TileType.Path => Color.white,
                    TileType.OccupiedObstacle => new Color(0.6f, 0.3f, 0f),
                    _ => GetZoneColor()
                };

                Gizmos.DrawCube(center, new Vector3(0.9f, 0.01f, 0.9f));
            }
        }

        Gizmos.color = new Color(1f, 0.4f, 0.8f);
        Vector3 endCenter = new Vector3((worldOffset.x + endingPos.x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + endingPos.y + 0.5f) * MasterManager.TileScale);
        Gizmos.DrawCube(endCenter, new Vector3(0.9f, 0.3f, 0.9f));
        
        if (pathGenerator != null && pathGenerator.spawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector2Int point in pathGenerator.spawnPoints)
            {
                Vector3 center = new Vector3((worldOffset.x + point.x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + point.y + 0.5f) * MasterManager.TileScale);
                Gizmos.DrawCube(center, new Vector3(0.5f, 0.5f, 0.5f));
            }
        }
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 spawnCenter = new Vector3((worldOffset.x + startingPos.x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + startingPos.y + 0.5f) * MasterManager.TileScale);
        Gizmos.DrawCube(spawnCenter, new Vector3(0.9f, 0.3f, 0.9f));

        if (branchGenerator != null && branchGenerator.edgeCandidates != null)
        {
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.4f);
            foreach (Vector2Int c in branchGenerator.edgeCandidates)
            {
                Vector3 center = new Vector3((worldOffset.x + c.x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + c.y + 0.5f) * MasterManager.TileScale);
                Gizmos.DrawCube(center, new Vector3(0.6f, 0.15f, 0.6f));
            }
        }

        if (branchGenerator != null && branchGenerator.branchSpawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector2Int bp in branchGenerator.branchSpawnPoints)
            {
                Vector3 center = new Vector3((worldOffset.x + bp.x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + bp.y + 0.5f) * MasterManager.TileScale);
                Gizmos.DrawCube(center, new Vector3(0.8f, 0.5f, 0.8f));
            }
        }

        Gizmos.color = new Color(0.34f, 0.54f, 0.76f);
        for (int x = 0; x < gridInstance.gridWidth; x++)
        {
            for (int z = 0; z < gridInstance.gridHeight; z++)
            {
                if (gridInstance.gridArray[x, z].type != TileType.Buildable) continue;
                Vector3 center = new Vector3((worldOffset.x + x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + z + 0.5f) * MasterManager.TileScale);
                Gizmos.DrawCube(center, new Vector3(0.8f, 0.05f, 0.8f));
            }
        }
    }

    private Color GetZoneColor()
    {
        return facingSide switch
        {
            Side.Bottom => Color.cyan,
            Side.Top => Color.yellow,
            Side.Right => Color.green,
            Side.Left => Color.red,
            _ => Color.white
        };
    }

    private void SetEndingPos()
    {
        int localMidx = (targetOffset.x + targetSize.x / 2) - worldOffset.x;
        int localMidz = (targetOffset.y + targetSize.y / 2) - worldOffset.y;
        switch (facingSide)
        {
            case Side.Bottom:
                endingPos = new Vector2Int(localMidx, 0);
                break;
            case Side.Top:
                endingPos = new Vector2Int(localMidx, gridInstance.gridHeight - 1);
                break;
            case Side.Left:
                endingPos = new Vector2Int(0, localMidz);
                break;
            case Side.Right:
                endingPos = new Vector2Int(gridInstance.gridWidth - 1, localMidz);
                break;
        }

        Tile tile = gridInstance.gridArray[endingPos.x, endingPos.y];
        tile.type = TileType.Path;
        gridInstance.gridArray[endingPos.x, endingPos.y] = tile;
    }

    private void GenerateWall()
    {
        switch (facingSide)
        {
            case Side.Bottom:
                for (int z = 0; z < gridInstance.gridHeight; z++)
                {
                    Tile tile = gridInstance.gridArray[gridInstance.gridWidth -1, z];
                    tile.type = TileType.Wall;
                    gridInstance.gridArray[gridInstance.gridWidth - 1, z] = tile;
                    Vector2Int pos = new Vector2Int(gridInstance.gridWidth - 1, z);
                    if(!wallTiles.Contains(pos))
                        wallTiles.Add(pos);
                }
                break;
            case Side.Top:
                for (int z = 0; z < gridInstance.gridHeight; z++)
                {
                    Tile tile = gridInstance.gridArray[0, z];
                    tile.type = TileType.Wall;
                    gridInstance.gridArray[0, z] = tile;
                    Vector2Int pos = new Vector2Int(0, z);
                    if (!wallTiles.Contains(pos))
                        wallTiles.Add(pos);
                }
                break;
            case Side.Right:
                for (int x = 0; x < gridInstance.gridWidth; x++)
                {
                    Tile tile = gridInstance.gridArray[x, gridInstance.gridHeight - 1];
                    tile.type = TileType.Wall;
                    gridInstance.gridArray[x, gridInstance.gridHeight - 1] = tile;
                    Vector2Int pos = new Vector2Int(x, gridInstance.gridHeight - 1);
                    if (!wallTiles.Contains(pos))
                        wallTiles.Add(pos);
                }
                break;
            case Side.Left:
                for (int x = 0; x < gridInstance.gridWidth; x++)
                {
                    Tile tile = gridInstance.gridArray[x, 0];
                    tile.type = TileType.Wall;
                    gridInstance.gridArray[x, 0] = tile;
                    Vector2Int pos = new Vector2Int(x, 0);
                    if (!wallTiles.Contains(pos))
                        wallTiles.Add(pos);
                }
                break;
        }  
    }

    private void GenerateOuterWall()
    {
        bool skipTop = facingSide == Side.Top;
        bool skipBottom = facingSide == Side.Bottom;
        bool skipLeft = facingSide == Side.Left;
        bool skipRight = facingSide == Side.Right;

        if (!skipTop)
            for (int x = 0; x < gridInstance.gridWidth; x++)
                MarkWall(x, gridInstance.gridHeight - 1);
        if (!skipBottom)
            for (int x = 0; x < gridInstance.gridWidth; x++)
                MarkWall(x, 0);
        if (!skipLeft)
            for (int z = 0; z < gridInstance.gridHeight; z++)
                MarkWall(0, z);
        if (!skipRight)
            for (int z = 0; z < gridInstance.gridHeight; z++)
                MarkWall(gridInstance.gridWidth - 1, z);
    }

    private void MarkWall(int x, int z)
    {
        Tile tile = gridInstance.gridArray[x, z];
        tile.type = TileType.Wall;
        gridInstance.gridArray[x, z] = tile;

        Vector2Int pos = new Vector2Int(x, z);
        if(!wallTiles.Contains(pos))
            wallTiles.Add(pos);
    }

    private void SetStartingPos()
    {
        startingPos = facingSide switch
        {
            Side.Bottom => new Vector2Int(1, gridInstance.gridHeight / 2),
            Side.Top => new Vector2Int(gridInstance.gridWidth - 2, gridInstance.gridHeight /2),
            Side.Right => new Vector2Int(gridInstance.gridWidth / 2, 1),
            Side.Left => new Vector2Int(gridInstance.gridWidth / 2, gridInstance.gridHeight - 2),
            _ => Vector2Int.zero
        };

        Tile tile = gridInstance.gridArray[startingPos.x, startingPos.y];
        tile.type = TileType.Path;
        gridInstance.gridArray[startingPos.x, startingPos.y] = tile;
    }

    public void GeneratePath(HashSet<Vector2Int> globalWalls, int branchCount, int minSpacing, int endpointExclusion)
    {
        pathGenerator = new ZonePathGenerator(gridInstance, endingPos, startingPos, facingSide, worldOffset, globalWalls);
        pathGenerator.Generate();

        branchGenerator = new BranchGenerator(gridInstance, endingPos, startingPos, facingSide, worldOffset, globalWalls);
        branchGenerator.GenerateBranchSpawners(branchCount, minSpacing, endpointExclusion);
        branchGenerator.GenerateBranches();

        List<Vector2Int> allSpawners = new List<Vector2Int> { startingPos };
        if(branchGenerator.branchSpawnPoints !=  null)
            allSpawners.AddRange(branchGenerator.branchSpawnPoints);

        pathGenerator.GenerateBuildables(allSpawners);
    }

    public void GenerateSpawners()
    {
        spawnerPlacer = new SpawnerPlacer(gridInstance, worldOffset, facingSide);
        spawnerPlacer.Place(startingPos, branchGenerator?.branchSpawnPoints);
    }

    public void BuildPath()
    {
        EnemyPathBuilder builder = new EnemyPathBuilder(gridInstance, worldOffset, facingSide);
        enemyPath = builder.Build(startingPos, endingPos);
    }

    public ZoneGridData GetSaveData()
    {
        ZoneGridData data = new ZoneGridData();
        data.width = gridInstance.gridWidth;
        data.height = gridInstance.gridHeight;
        data.worldOffset = worldOffset;
        data.facingSide = facingSide;
        data.endingPos = endingPos;
        data.startingPos = startingPos;
        data.spawnpoints = pathGenerator.spawnPoints;
        data.branchSpawnPoints = branchGenerator?.branchSpawnPoints ?? new List<Vector2Int>();
        data.branchMergeTiles = branchGenerator?.branchMergeTiles ?? new List<Vector2Int>();
        data.tiles = new List<TileData>();
        data.obstacles = obstaclePlacer?.placedObstacles ?? new List<ObstaclePlacementData>();
        data.spawners = spawnerPlacer?.placedSpawners ?? new List<SpawnerData>();
        

        for (int x = 0; x < gridInstance.gridWidth; x++)
            for (int z = 0; z < gridInstance.gridHeight; z++)
            {
                TileData tile = new TileData();
                tile.x = x;
                tile.z = z;
                tile.tileType = gridInstance.gridArray[x, z].type;
                data.tiles.Add(tile);
            }

        return data;
    }

    public void LoadFromData(ZoneGridData data)
    {
        worldOffset = data.worldOffset;
        facingSide = data.facingSide;
        endingPos = data.endingPos;
        startingPos = data.startingPos;

        gridInstance = new Grid(data.width, data.height);

        foreach (TileData tile in data.tiles)
        {
            Tile t = gridInstance.gridArray[tile.x, tile.z];
            t.type = tile.tileType;
            gridInstance.gridArray[tile.x, tile.z] = t;
        }

        wallTiles = new List<Vector2Int>();
        for (int x = 0; x < gridInstance.gridWidth; x++)
            for (int z = 0; z < gridInstance.gridHeight; z++)
                if (gridInstance.gridArray[x, z].type == TileType.Wall)
                    wallTiles.Add(new Vector2Int(x, z));

        pathGenerator = new ZonePathGenerator(gridInstance, endingPos, startingPos, facingSide, worldOffset, null);
        pathGenerator.spawnPoints = data.spawnpoints;

        branchGenerator = new BranchGenerator(gridInstance, endingPos, startingPos, facingSide, worldOffset, null);
        branchGenerator.branchSpawnPoints = data.branchSpawnPoints ?? new List<Vector2Int>();
        branchGenerator.branchMergeTiles = data.branchMergeTiles ?? new List<Vector2Int>();

        obstaclePlacer = new ObstaclePlacer(gridInstance, obstacleCatalogue, worldOffset);
        obstaclePlacer.placedObstacles = data.obstacles ?? new List<ObstaclePlacementData>();

        spawnerPlacer = new SpawnerPlacer(gridInstance, worldOffset, facingSide);
        spawnerPlacer.placedSpawners = data.spawners ?? new List<SpawnerData>();

    }

    public void GenerateObstacles()
    {
        for (int x = 0; x < gridInstance.gridWidth; x++)
        {
            for(int z = 0;z < gridInstance.gridHeight; z++)
            {
                if (gridInstance.gridArray[x, z].type == TileType.OccupiedObstacle)
                    SetTileType(new Vector2Int(x, z), TileType.Obstacle);
            }
        }
        obstaclePlacer = new ObstaclePlacer(gridInstance, obstacleCatalogue, worldOffset);
        obstaclePlacer.Place(obstacleDensity, obstacleMinSpacing);
    }

    private void SetTileType(Vector2Int pos, TileType type)
    {
        Tile tile = gridInstance.gridArray[pos.x, pos.y];
        tile.type = type;
        gridInstance.gridArray[pos.x, pos.y] = tile;
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MasterManager : MonoBehaviour
{
    [SerializeField] private TargetManager targetManager;
    [SerializeField] private ZoneGridManager topGrid;
    [SerializeField] private ZoneGridManager bottomGrid;
    [SerializeField] private ZoneGridManager leftGrid;
    [SerializeField] private ZoneGridManager rightGrid;
    [SerializeField] private MapVisualizer targetVisualizer;
    [SerializeField] private MapVisualizer topVisualizer;
    [SerializeField] private MapVisualizer bottomVisualizer;
    [SerializeField] private MapVisualizer leftVisualizer;
    [SerializeField] private MapVisualizer rightVisualizer;

    [SerializeField] private int zoneDepth;

    [SerializeField] private int branchCount = 3;
    [SerializeField] private int branchSpawnerMinSpacing = 8;
    [SerializeField] private int branchEndpointExclusion = 10;

    [SerializeField] private ObstacleManager obstacleManager;

    [SerializeField] private WallManager wallManager;

    private HashSet<Vector2Int> globalWallPositions;

    private string savePath => Application.persistentDataPath + "/fullmap.json";

    [ContextMenu("Generate Full Map")]
    private void GenerateMap()
    {
#if UNITY_EDITOR
        if(Application.isPlaying)
        {
            Debug.LogWarning("Cannot generate map in play mode. Exit play mode first.");
            return;
        }

        targetManager.Initialize();
        Vector2Int targetSize = targetManager.Size;
        Vector2Int targetOffset = targetManager.WorldOffset;
        

        topGrid.Initialize(targetSize.x + zoneDepth, zoneDepth, targetOffset + new Vector2Int(-zoneDepth, targetSize.y), Side.Bottom, targetSize, targetOffset, targetOffset + new Vector2Int(-zoneDepth, targetSize.y), null);
        bottomGrid.Initialize(targetSize.x + zoneDepth, zoneDepth, targetOffset + new Vector2Int(0, -zoneDepth), Side.Top, targetSize, targetOffset, targetOffset + new Vector2Int(0, -zoneDepth), null);
        leftGrid.Initialize(zoneDepth, targetSize.y + zoneDepth, targetOffset + new Vector2Int(-zoneDepth, -zoneDepth), Side.Right, targetSize, targetOffset, targetOffset + new Vector2Int(-zoneDepth, -zoneDepth), null);
        rightGrid.Initialize(zoneDepth, targetSize.y + zoneDepth, targetOffset + new Vector2Int(targetSize.x, 0), Side.Left, targetSize, targetOffset, targetOffset + new Vector2Int(targetSize.x, 0), null);

        CollectGlobalWalls();
        topGrid.GeneratePath(globalWallPositions, branchCount, branchSpawnerMinSpacing, branchEndpointExclusion);
        bottomGrid.GeneratePath(globalWallPositions, branchCount, branchSpawnerMinSpacing, branchEndpointExclusion);
        leftGrid.GeneratePath(globalWallPositions, branchCount, branchSpawnerMinSpacing, branchEndpointExclusion);
        rightGrid.GeneratePath(globalWallPositions, branchCount, branchSpawnerMinSpacing, branchEndpointExclusion);

        topGrid.GenerateObstacles();
        bottomGrid.GenerateObstacles();
        rightGrid.GenerateObstacles();
        leftGrid.GenerateObstacles();

        obstacleManager.SpawnObstacles(topGrid.obstaclePlacer, topGrid.obstacleCatalogue, topGrid.worldOffset, Side.Top);
        obstacleManager.SpawnObstacles(bottomGrid.obstaclePlacer, bottomGrid.obstacleCatalogue, bottomGrid.worldOffset, Side.Bottom);
        obstacleManager.SpawnObstacles(rightGrid.obstaclePlacer, rightGrid.obstacleCatalogue, rightGrid.worldOffset, Side.Right);
        obstacleManager.SpawnObstacles(leftGrid.obstaclePlacer, leftGrid.obstacleCatalogue, leftGrid.worldOffset, Side.Left);

        wallManager.ClearAll();

        wallManager.SpawnZoneWalls(topGrid, Side.Top);
        wallManager.SpawnZoneWalls(bottomGrid, Side.Bottom);
        wallManager.SpawnZoneWalls(rightGrid, Side.Right);
        wallManager.SpawnZoneWalls(leftGrid, Side.Left);
        wallManager.SpawnTargetWalls(targetManager);

        Visualise();
#endif
    }

    [ContextMenu("Save Map")]
    private void Save()
    {
        GridData targetData = targetManager.GetSaveData();
        if (targetData == null)
        {
            Debug.LogWarning("Save aborted — map data is missing. Generate the map first.");
            return;
        }

        FullMapData data = new FullMapData();
        data.targetData = targetManager.GetSaveData();
        data.topData = topGrid.GetSaveData();
        data.bottomData = bottomGrid.GetSaveData();
        data.leftData = leftGrid.GetSaveData();
        data.rightData = rightGrid.GetSaveData();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    [ContextMenu("Load Map")]
    private void Load()
    {
        string json = File.ReadAllText(savePath);
        FullMapData data = JsonUtility.FromJson<FullMapData>(json);

        targetManager.LoadFromData(data.targetData);

        topGrid.LoadFromData(data.topData);
        bottomGrid.LoadFromData(data.bottomData);
        leftGrid.LoadFromData(data.leftData);
        rightGrid.LoadFromData(data.rightData);

        obstacleManager.SpawnObstacles(topGrid.obstaclePlacer, topGrid.obstacleCatalogue, topGrid.worldOffset, Side.Top);
        obstacleManager.SpawnObstacles(bottomGrid.obstaclePlacer, bottomGrid.obstacleCatalogue, bottomGrid.worldOffset, Side.Bottom);
        obstacleManager.SpawnObstacles(rightGrid.obstaclePlacer, rightGrid.obstacleCatalogue, rightGrid.worldOffset, Side.Right);
        obstacleManager.SpawnObstacles(leftGrid.obstaclePlacer, leftGrid.obstacleCatalogue, leftGrid.worldOffset, Side.Left);

        wallManager.SpawnZoneWalls(topGrid, Side.Top);
        wallManager.SpawnZoneWalls(bottomGrid, Side.Bottom);
        wallManager.SpawnZoneWalls(rightGrid, Side.Right);
        wallManager.SpawnZoneWalls(leftGrid, Side.Left);
        wallManager.SpawnTargetWalls(targetManager);
    }

    private void Visualise()
    {
        targetVisualizer.VisualiseTarget(targetManager);
        topVisualizer.Visualise(topGrid.Grid, topGrid.worldOffset);
        bottomVisualizer.Visualise(bottomGrid.Grid, bottomGrid.worldOffset);
        rightVisualizer.Visualise(rightGrid.Grid, rightGrid.worldOffset);
        leftVisualizer.Visualise(leftGrid.Grid, leftGrid.worldOffset);

        targetVisualizer.CombineTiles();
        topVisualizer.CombineTiles();
        bottomVisualizer.CombineTiles();
        leftVisualizer.CombineTiles();
        rightVisualizer.CombineTiles();
    }

    private bool SaveExists() => File.Exists(savePath);

    private void Start()
    {
        if (SaveExists())
            Load();
        else
            Debug.LogError("No save file found. Generate and save the map in editor before entering play mode.");
    }

    private void CollectGlobalWalls()
    {
        globalWallPositions = new HashSet<Vector2Int>();

        foreach(Vector2Int w in targetManager.wallTiles)
            globalWallPositions.Add(w + targetManager.WorldOffset);

        foreach (ZoneGridManager zone in new[] { topGrid, bottomGrid, leftGrid, rightGrid })
            foreach (Vector2Int w in zone.wallTiles)
                globalWallPositions.Add(w + zone.worldOffset);

        foreach (Vector2Int b in targetManager.BorderPositions())
            globalWallPositions.Add(b);
    }
}

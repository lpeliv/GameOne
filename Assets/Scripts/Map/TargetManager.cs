using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;

    [SerializeField] private Vector2Int worldOffset;

    private Grid targetGrid;
    public Grid Grid => targetGrid;

    [SerializeField] private int healthBudCount = 4;
    public List<Vector2Int> healthBudPositions;

    [SerializeField] private int innerSize = 4;

    public List<Vector2Int> wallTiles;
    private List<Vector2Int> houseTiles;

    private int innerStart;
    private int innerEnd;
    private int N;

    public int InnerStart => innerStart;
    public int InnerEnd => innerEnd;

    Dictionary<Vector2Int, Side> tileZones;
    public Dictionary<Vector2Int, Side> TileZones => tileZones;

    public Vector2Int Size => new Vector2Int(width, height);
    public Vector2Int WorldOffset => worldOffset;

    [ContextMenu("Generate Target")]
    private void GenerateTarget()
    {
        Initialize();
    }

    public void Initialize()
    {
        targetGrid = new Grid(width, height);
        healthBudPositions = new List<Vector2Int>();
        wallTiles = new List<Vector2Int>();
        houseTiles = new List<Vector2Int>();

        N = Mathf.Min(width, height);
        innerStart = N / 2 - innerSize / 2;
        innerEnd = N / 2 + innerSize / 2 - 1;

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                Tile tile = targetGrid.gridArray[x, z];
                tile.type = TileType.Target;
                targetGrid.gridArray[x, z] = tile;
            }
        GenerateWalls();
        GenerateHouseArea();
        AssignZones();
        PlaceHealthBuds(Side.Left);
        PlaceHealthBuds(Side.Right);
        PlaceHealthBuds(Side.Top);
        PlaceHealthBuds(Side.Bottom);
    }

    private void OnDrawGizmos()
    {
        if (targetGrid != null)
        {
            for (int x = 0; x <= targetGrid.gridWidth; x++)
            {
                Gizmos.DrawLine(
                    new Vector3((worldOffset.x + x) * MasterManager.TileScale, 0, worldOffset.y * MasterManager.TileScale),
                    new Vector3((worldOffset.x + x) * MasterManager.TileScale, 0, (worldOffset.y + targetGrid.gridHeight) * MasterManager.TileScale));
            }
            for (int z = 0; z <= targetGrid.gridHeight; z++)
            {
                Gizmos.DrawLine(
                    new Vector3(worldOffset.x * MasterManager.TileScale, 0, (worldOffset.y + z) * MasterManager.TileScale),
                    new Vector3((worldOffset.x + targetGrid.gridWidth) * MasterManager.TileScale, 0, (worldOffset.y + z) * MasterManager.TileScale));
            }

            for (int x = 0; x < targetGrid.gridWidth; x++)
            {
                for (int z = 0; z < targetGrid.gridHeight; z++)
                {
                    Vector3 center = new Vector3((worldOffset.x + x + 0.5f) * MasterManager.TileScale, 0, (worldOffset.y + z + 0.5f) * MasterManager.TileScale);
                    Vector2Int pos = new Vector2Int(x, z);
                    Tile tile = targetGrid.gridArray[x, z];

                    if (tile.type == TileType.Wall) continue;
                    if (tile.type == TileType.House) continue;

                    if (tileZones != null && tileZones.TryGetValue(pos, out Side side))
                    {
                        Gizmos.color = side switch
                        {
                            Side.Top => Color.cyan,
                            Side.Bottom => Color.yellow,
                            Side.Left => Color.green,
                            Side.Right => Color.red,
                            _ => Color.white
                        };
                    }
                    else
                    {
                        Gizmos.color = Color.magenta;
                    }
                    Gizmos.DrawCube(center, new Vector3(0.9f, 0.01f, 0.9f) * MasterManager.TileScale);
                }
            }

            if (healthBudPositions != null && healthBudPositions.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                foreach (Vector2Int budPos in healthBudPositions)
                {
                    Vector3 center = new Vector3(
                        (worldOffset.x + budPos.x + 0.5f) * MasterManager.TileScale,
                        0,
                        (worldOffset.y + budPos.y + 0.5f) * MasterManager.TileScale);
                    Gizmos.DrawCube(center, new Vector3(0.9f, 0.01f, 0.9f) * MasterManager.TileScale);
                }
            }

            if (wallTiles != null && wallTiles.Count > 0)
            {
                Gizmos.color = new Color(0f, 0f, 0f);
                foreach (Vector2Int xLine in wallTiles)
                {
                    Vector3 center = new Vector3(
                        (worldOffset.x + xLine.x + 0.5f) * MasterManager.TileScale,
                        0,
                        (worldOffset.y + xLine.y + 0.5f) * MasterManager.TileScale);
                    Gizmos.DrawCube(center, new Vector3(0.9f, 0.01f, 0.9f) * MasterManager.TileScale);
                }
            }

            if (houseTiles != null && houseTiles.Count > 0)
            {
                Gizmos.color = new Color(0.4f, 0.3f, 0.6f);
                foreach (Vector2Int area in houseTiles)
                {
                    Vector3 center = new Vector3(
                        (worldOffset.x + area.x + 0.5f) * MasterManager.TileScale,
                        0,
                        (worldOffset.y + area.y + 0.5f) * MasterManager.TileScale);
                    Gizmos.DrawCube(center, new Vector3(0.9f, 0.01f, 0.9f) * MasterManager.TileScale);
                }
            }
        }
    }

    private void PlaceHealthBuds(Side side)
    {
        List<Vector2Int> borderTiles = new List<Vector2Int>();

        switch (side)
        {
            case Side.Left:
                for(int z = 2; z < height -2; z++)
                    borderTiles.Add(new Vector2Int(0, z));
                break;
            case Side.Right:
                for (int z = 2; z < height - 2; z++)
                    borderTiles.Add(new Vector2Int(width - 1, z));
                break;
            case Side.Top:
                for (int x = 2; x < width - 2; x++)
                    borderTiles.Add(new Vector2Int(x, height - 1));
                break;
            case Side.Bottom:
                for (int x = 2; x < height - 2; x++)
                    borderTiles.Add(new Vector2Int(x, 0));
                break;
        }   

        for (int i = 0; i < healthBudCount; i++)
        {
            if (borderTiles.Count == 0) break;
            int index = Random.Range(0, borderTiles.Count);
            healthBudPositions.Add(borderTiles[index]);
            borderTiles.RemoveAt(index);
        }
    }

    private void GenerateWalls()
    {
        for(int i = 0; i < N; i++)
        {
            if (i >= innerStart && i <= innerEnd)
                continue;

            Tile tile = targetGrid.gridArray[i, i];
            tile.type = TileType.Wall;
            targetGrid.gridArray[i, i] = tile;
            wallTiles.Add(new Vector2Int(tile.x, tile.z));
            
            if(i != N - 1 - i)
            {
                tile = targetGrid.gridArray[i, N - 1 - i];
                tile.type = TileType.Wall;
                targetGrid.gridArray[i, N - 1 - i] = tile;
                wallTiles.Add(new Vector2Int(tile.x, tile.z));
            }
        }
    }

    private void GenerateHouseArea()
    {
        for (int i = innerStart; i <= innerEnd; i++)
        {
            for(int j = innerStart; j <= innerEnd; j++)
            {
                    Tile tile = targetGrid.gridArray[i, j];
                    tile.type = TileType.House;
                    targetGrid.gridArray[i, j] = tile;
                    houseTiles.Add(new Vector2Int(tile.x, tile.z));
            }
        }
    }

    private void AssignZones()
    {
        tileZones = new Dictionary<Vector2Int, Side>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Tile tile = targetGrid.gridArray[x, z];
                if (tile.type == TileType.Wall || tile.type == TileType.House)
                    continue;

                Side side = GetSideZone(x, z, N);
                tileZones[new Vector2Int(x, z)] = side;
            }
        }
    }

    private Side GetSideZone(int x, int z, int N)
    {
        if(z > x && z > (N - 1 - x))
            return Side.Top;
        else if (z < x && z < (N - 1 - x))
            return Side.Bottom;
        else if (z > x && z < (N - 1 - x))
            return Side.Left;
        else return Side.Right;
    }

    public HashSet<Vector2Int> BorderPositions()
    {
        HashSet<Vector2Int> border = new HashSet<Vector2Int>();
        for(int x = 0; x < width; x++)
        {
            border.Add(worldOffset + new Vector2Int(x, 0));
            border.Add(worldOffset + new Vector2Int(x, height - 1));
        }
        for(int z  = 0; z < height; z++)
        {
            border.Add(worldOffset + new Vector2Int(0, z));
            border.Add(worldOffset + new Vector2Int(width -1, z));
        }
        return border;
    }

    public GridData GetSaveData()
    {
        if (targetGrid == null)
        {
            Debug.LogWarning("TargetManager has no grid data to save. Generate the map first.");
            return null;
        }

        GridData data = new GridData();
        data.width = width;
        data.height = height;
        data.startpos = Vector2Int.zero;
        data.endpos = Vector2Int.zero;
        data.spawnpoints = new List<Vector2Int>(healthBudPositions);
        data.tiles = new List<TileData>();

        for(int x = 0; x < width; x++)
            for(int z = 0;z < height; z++)
            {
                data.tiles.Add(new TileData
                {
                    x = x,
                    z = z,
                    tileType = targetGrid.gridArray[x, z].type,
                });
            }
        return data;
    }

    public void LoadFromData(GridData data)
    {
        width = data.width;
        height = data.height;
        targetGrid = new Grid(width, height);
        wallTiles = new List<Vector2Int>();
        houseTiles = new List<Vector2Int>();
        healthBudPositions = new List<Vector2Int>(data.spawnpoints);
        tileZones = new Dictionary<Vector2Int, Side>();

        foreach(TileData tileData in data.tiles)
        {
            Tile tile = targetGrid.gridArray[tileData.x, tileData.z];
            tile.type = tileData.tileType;
            targetGrid.gridArray[tileData.x, tileData.z] = tile;

            Vector2Int pos = new Vector2Int(tileData.x, tileData.z);
            switch (tileData.tileType)
            {
                case TileType.Wall:
                    wallTiles.Add(pos); break;
                case TileType.House:
                    houseTiles.Add(pos); break;
            }
        }

        int N = Mathf.Min(width, height);
        innerStart = N / 2 - innerSize / 2;
        innerEnd = N / 2 + innerSize / 2 - 1;
        AssignZones();
    }
}

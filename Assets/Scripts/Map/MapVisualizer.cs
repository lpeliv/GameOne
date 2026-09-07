using System.Collections.Generic;
using UnityEngine;

public class MapVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject pathTilePrefab;
    [SerializeField] private GameObject obstacleTilePrefab;
    [SerializeField] private GameObject buildableTilePrefab;
    [SerializeField] private GameObject wallTilePrefab;
    [SerializeField] private GameObject targetTopPrefab;
    [SerializeField] private GameObject targetBottomPrefab;
    [SerializeField] private GameObject targetLeftPrefab;
    [SerializeField] private GameObject targetRightPrefab;
    [SerializeField] private GameObject houseTilePrefab;
    [SerializeField] private GameObject healthBudPrefab;
    [SerializeField] private Material healthBudMaterial;

    private Dictionary<TileType, GameObject> prefabMap;

    private void Awake()
    {
        prefabMap = new Dictionary<TileType, GameObject>
        {
            {TileType.Path, pathTilePrefab},
            {TileType.Obstacle, obstacleTilePrefab},
            {TileType.Buildable, buildableTilePrefab},
            {TileType.Wall, wallTilePrefab},
        };
    }

    public void Visualise(Grid grid, Vector2Int worldOffset)
    {
        prefabMap = new Dictionary<TileType, GameObject>
        {
            {TileType.Path, pathTilePrefab},
            {TileType.Obstacle, obstacleTilePrefab},
            {TileType.OccupiedObstacle, obstacleTilePrefab},
            {TileType.Buildable, buildableTilePrefab},
            {TileType.Wall, wallTilePrefab},
        };

        ClearChildren();

        for(int x = 0; x < grid.gridWidth; x++)
        {
            for(int z = 0; z < grid.gridHeight; z++)
            {
                TileType type = grid.gridArray[x, z].type;

                if (!prefabMap.TryGetValue(type, out GameObject prefab)) continue;
                if (prefab == null) continue;

                Vector3 worldPos = new Vector3(
                    (worldOffset.x + x + 0.5f) * MasterManager.TileScale,
                    0f,
                    (worldOffset.y + z + 0.5f) * MasterManager.TileScale
                );

                Instantiate(prefab, worldPos, Quaternion.identity, transform);
            } 
        }
    }

    private void ClearChildren()
    {
#if UNITY_EDITOR
        for (int i = transform.childCount - 1; i >= 0; i--)
        DestroyImmediate(transform.GetChild(i).gameObject);
#endif
    }

    public void VisualiseTarget(TargetManager targetManager)
    {
        ClearChildren();

        Vector2Int worldOffset = targetManager.WorldOffset;
        Grid grid = targetManager.Grid;

        for (int x = 0; x < grid.gridWidth; x++)
        {
            for (int z = 0; z < grid.gridHeight; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Tile tile = grid.gridArray[x, z];

                Vector3 worldPos = new Vector3(
                    (worldOffset.x + x + 0.5f) * MasterManager.TileScale,
                    0f,
                    (worldOffset.y + z + 0.5f) * MasterManager.TileScale
                );

                switch (tile.type)
                {
                    case TileType.Wall:
                        Instantiate(wallTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.House:
                        Instantiate(houseTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                    case TileType.Target:
                        if (!targetManager.TileZones.TryGetValue(pos, out Side side)) break;
                        GameObject targetPrefab = side switch
                        {
                            Side.Top => targetTopPrefab,
                            Side.Bottom => targetBottomPrefab,
                            Side.Left => targetLeftPrefab,
                            Side.Right => targetRightPrefab,
                            _ => null,
                        };

                        if(targetPrefab != null)
                            Instantiate(targetPrefab, worldPos, Quaternion.identity, transform);
                        break;

                }
            }
        }

        foreach (Vector2Int budPos in targetManager.healthBudPositions)
        {
            Vector3 worldPos = new Vector3(
                (worldOffset.x + budPos.x + 0.5f) * MasterManager.TileScale,
                0.2f,
                (worldOffset.y + budPos.y + 0.5f) * MasterManager.TileScale
            );

            Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
            GameObject go = Instantiate(healthBudPrefab, worldPos, rotation, transform);
            HealthBud bud = go.GetComponent<HealthBud>();

            if (bud != null)
            {
                if (targetManager.TileZones.TryGetValue(budPos, out Side budZone))
                {
                    bud.zone = budZone;
                }
            }
        }
    }

    public void CombineTiles()
    {
        List<GameObject> originals = new List<GameObject>();
        Dictionary<Material, List<MeshFilter>> groups = new Dictionary<Material, List<MeshFilter>>();

        foreach(Transform child in transform)
        {
            MeshFilter mf = child.GetComponent<MeshFilter>();
            MeshRenderer mr = child.GetComponent<MeshRenderer>();

            if (mf == null || mr == null) continue;
            if (mr.sharedMaterial == healthBudMaterial) continue;
            if (child.GetComponent<BuildableTile>() != null) continue;
            if (mf.sharedMesh == null) continue;
            if (!mf.sharedMesh.isReadable)
            {
                continue;
            }

            originals.Add(child.gameObject);
            Material mat = mr.sharedMaterial;

            if(!groups.ContainsKey(mat))
                groups[mat] = new List<MeshFilter>();

            groups[mat].Add(mf);
        }

        foreach (var group in groups)
        {
            Material mat = group.Key;
            List<MeshFilter> filters = group.Value;

            CombineInstance[] combine = new CombineInstance[filters.Count];

            for(int i = 0; i < filters.Count; i++)
            {
                combine[i].mesh = filters[i].sharedMesh;
                combine[i].transform = filters[i].transform.localToWorldMatrix;
            }

            GameObject combined = new GameObject($"Combined_{mat.name}");
            combined.transform.SetParent(transform);
            combined.transform.localPosition = Vector3.zero;
            combined.transform.localRotation = Quaternion.identity;
            combined.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh();

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine, true, true);

            MeshFilter combinedMF = combined.AddComponent<MeshFilter>();
            combinedMF.sharedMesh = mesh;

            MeshRenderer combinedMR = combined.AddComponent<MeshRenderer>();
            combinedMR.sharedMaterial = mat;

            MeshCollider col = combined.AddComponent<MeshCollider>();   
            col.sharedMesh = mesh;
            col.convex = false;
        }

        foreach (GameObject go in originals)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);

        }
    }
}

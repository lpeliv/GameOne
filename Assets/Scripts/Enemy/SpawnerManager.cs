using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject mainSpawnerPrefab;
    [SerializeField] private GameObject branchSpawnerPrefab;

    [Header("Zone Containers")]
    [SerializeField] private Transform topContainer;
    [SerializeField] private Transform bottomContainer;
    [SerializeField] private Transform leftContainer;
    [SerializeField] private Transform rightContainer;

    private readonly Dictionary<Side, List<EnemySpawner>> spawnerByZone = new Dictionary<Side, List<EnemySpawner>>
    {
        {Side.Top, new List<EnemySpawner>() },
        {Side.Bottom, new List<EnemySpawner>() },
        {Side.Left, new List<EnemySpawner>() },
        {Side.Right, new List<EnemySpawner>() },
    };

    private void Awake()
    {
        ClearAll();
    }

    public void SpawnSpawners(SpawnerPlacer placer, Side zone)
    {
        if(placer ?.placedSpawners == null)
        {
            Debug.LogWarning($"[SpawnerManager] No placed spawner data for zone {zone}.");
            return;
        }

        Transform container = GetContainer(zone);
        if(container == null)
        {
            Debug.LogWarning($"[SpawnerManager] No container assigned for zone {zone}.");
            return;
        }

        ClearContainer(container, zone);

        foreach (SpawnerData data in placer.placedSpawners)
            InstantiateSpawner(data, container, zone);
    }

    public void SpawnSpawnersFromData(List<SpawnerData> savedSpawners, Side zone)
    {
        if (savedSpawners == null || savedSpawners.Count == 0) return;

        Transform container = GetContainer(zone);
        if (container == null)
        {
            Debug.LogWarning($"[SpawnerManager] No container assigned for zone {zone}.");
            return;
        }

        ClearContainer(container, zone);

        foreach (SpawnerData data in savedSpawners)
            InstantiateSpawner(data, container, zone);
    }

    public IReadOnlyList<EnemySpawner> GetSpawnerForZone(Side zone) => 
        spawnerByZone.TryGetValue(zone, out List<EnemySpawner> list)
        ? list : System.Array.Empty<EnemySpawner>();

    public void ClearAll()
    {
        foreach (Side side in spawnerByZone.Keys)
            ClearContainer(GetContainer(side), side);
    }

    private void InstantiateSpawner(SpawnerData data, Transform container, Side zone)
    {
        GameObject prefab = data.spawnerType == SpawnerType.Main
            ? mainSpawnerPrefab : branchSpawnerPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnerManager] Prefab for {data.spawnerType} is not assigned.");
            return;
        }

        GameObject go = Instantiate(prefab, data.worldPos, Quaternion.identity, container);
        go.name = $"Spawner_{zone}_{data.gridPos}";

        EnemySpawner spawner = go.GetComponent<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogError($"[SpawnerManager] Prefab '{prefab.name}' is missing an EnemySpawner component.");
            return;
        }

        spawner.Initialize(data);
        spawnerByZone[zone].Add(spawner);
    }

    private void ClearContainer(Transform container, Side zone)
    {
        if (container == null) return;

        if (spawnerByZone.TryGetValue(zone, out List<EnemySpawner> list))
            list.Clear();

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private Transform GetContainer(Side zone) => zone switch
    {
        Side.Top => topContainer,
        Side.Bottom => bottomContainer,
        Side.Left => leftContainer,
        Side.Right => rightContainer,
        _ => null,
    };
}

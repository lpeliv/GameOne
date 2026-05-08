using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject zoneWallPrefab;
    [SerializeField] private GameObject targetWallPrefab;

    [Header("Height Settings")]
    [SerializeField] private float maxWallHeight = 6f;
    [SerializeField] private float minWallHeight = 2f;

    [Header("Containers")]
    [SerializeField] private Transform topWallContainer;
    [SerializeField] private Transform bottomWallContainer;
    [SerializeField] private Transform rightWallContainer;
    [SerializeField] private Transform leftWallContainer;
    [SerializeField] private Transform targetWallContainer;

    private Dictionary<Side, Transform> zoneContainers;

    private void Awake()
    {
        Initialize();
        ClearAll();
    }

    private void Initialize()
    {
        if (zoneContainers != null) return;

        zoneContainers = new Dictionary<Side, Transform>
        {
            {Side.Top, topWallContainer },
            {Side.Bottom, bottomWallContainer },
            {Side.Left, leftWallContainer },
            {Side.Right, rightWallContainer },
        };
    }

    public void SpawnZoneWalls(ZoneGridManager zone, Side side)
    {
        if (zoneContainers == null) Initialize();
        Transform container = zoneContainers[side];

        ClearContainer(container);

        foreach (Vector2Int localPos in zone.WallTiles)
        {
            Vector3 worldPos = new Vector3(
                zone.worldOffset.x + localPos.x + 0.5f,
                maxWallHeight / 2f,
                zone.worldOffset.y + localPos.y + 0.5f
            );

             GameObject wall = Instantiate(zoneWallPrefab, worldPos, Quaternion.identity, container);

            wall.transform.localScale = new Vector3(1f, maxWallHeight, 1f);
        }

    }

    public void ClearAll()
    {
        if(zoneContainers ==  null) Initialize();
        foreach (var kvp in zoneContainers)
        foreach (Transform container in zoneContainers.Values)
            ClearContainer(container);


        ClearContainer(targetWallContainer);
    }

    private void ClearContainer(Transform container)
    {
        if(container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    public void SpawnTargetWalls(TargetManager targetManager)
    {
        ClearContainer(targetWallContainer);

        Vector2Int worldOffset = targetManager.WorldOffset;
        int innerStart = targetManager.InnerStart;
        int innerEnd = targetManager.InnerEnd;

        int gridSize = Mathf.Min(targetManager.Size.x, targetManager.Size.y);
        float maxDistance = 0f;
        maxDistance = Mathf.Max(innerStart, gridSize - 1 - innerEnd);

        foreach(Vector2Int localPos in targetManager.wallTiles)
        {
            float distToHouse = GetDistanceToHouseBoundary(localPos, innerStart, innerEnd);

            if (distToHouse > maxDistance)
                maxDistance = distToHouse;

            float t = Mathf.Clamp01(Mathf.Pow(distToHouse / maxDistance, 2f));
            float height = Mathf.Lerp(minWallHeight, maxWallHeight, t);

            Vector3 worldPos = new Vector3(
                worldOffset.x + localPos.x + 0.5f,
                height / 2f,
                worldOffset.y + localPos.y + 0.5f
            );

            GameObject wall = Instantiate(targetWallPrefab, worldPos, Quaternion.identity, targetWallContainer);
            wall.transform.localScale = new Vector3(1f, height, 1f);
        }

        if (maxDistance < 0.001f) maxDistance = 1f;
    }

    private float GetDistanceToHouseBoundary(Vector2Int pos, int innerStart, int innerEnd)
    {
        float distX = 0f;
        if(pos.x < innerStart)
            distX = innerStart - pos.x;
        else if(pos.x > innerEnd)
            distX = pos.x - innerEnd;

        float distZ = 0f;
        if(pos.y < innerStart)
            distZ = innerStart - pos.y;
        else if(pos.y > innerEnd)
            distZ = pos.y - innerEnd;

        return distX + distZ;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class BranchObstacleManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject branchObstaclePrefab;

    [Header("Zone Containers")]
    [SerializeField] private Transform topContainer;
    [SerializeField] private Transform bottomContainer;
    [SerializeField] private Transform leftContainer;
    [SerializeField] private Transform rightContainer;

    private readonly Dictionary<Side, List<BranchObstacle>> obstaclesByZone =
        new Dictionary<Side, List<BranchObstacle>>
        {
            { Side.Top,    new List<BranchObstacle>() },
            { Side.Bottom, new List<BranchObstacle>() },
            { Side.Left,   new List<BranchObstacle>() },
            { Side.Right,  new List<BranchObstacle>() },
        };

    private void Awake()
    {
        ClearAll();
    }

    public void PlaceObstacles(ZoneGridManager zone, Side side)
    {
        if (branchObstaclePrefab == null)
        {
            Debug.LogWarning("[BranchObstacleManager] No prefab assigned.");
            return;
        }

        Transform container = GetContainer(side);
        if (container == null)
        {
            Debug.LogWarning($"[BranchObstacleManager] No container assigned for zone {side}.");
            return;
        }

        ClearContainer(container, side);

        List<Vector2Int> mergeTiles = zone.branchGenerator?.branchMergeTiles;
        if (mergeTiles == null || mergeTiles.Count == 0)
        {
            Debug.LogWarning($"[BranchObstacleManager] No merge tiles found for zone {side}.");
            return;
        }

        for (int i = 0; i < mergeTiles.Count; i++)
        {
            Vector2Int localPos = mergeTiles[i];

            Vector3 worldPos = new Vector3(
                (zone.worldOffset.x + localPos.x + 0.5f) * MasterManager.TileScale,
                1f,
                (zone.worldOffset.y + localPos.y + 0.5f) * MasterManager.TileScale
            );

            GameObject go = Instantiate(branchObstaclePrefab, worldPos, Quaternion.identity, container);
            go.name = $"BranchObstacle_{side}_{i}";

            BranchObstacle obstacle = go.GetComponent<BranchObstacle>();
            if (obstacle == null)
            {
                Debug.LogError("[BranchObstacleManager] Prefab missing BranchObstacle component.");
                continue;
            }

            obstacle.Initialize(localPos, i);
            obstaclesByZone[side].Add(obstacle);
        }

        Debug.Log($"[BranchObstacleManager] Placed {mergeTiles.Count} branch obstacles for zone {side}.");
    }

    public BranchObstacle GetObstacleForBranch(Side side, int branchIndex)
    {
        if (!obstaclesByZone.TryGetValue(side, out List<BranchObstacle> list))
            return null;

        foreach (BranchObstacle obstacle in list)
            if (obstacle != null && obstacle.BranchIndex == branchIndex)
                return obstacle;

        return null;
    }

    public void RemoveObstacleForBranch(Side side, int branchIndex)
    {
        BranchObstacle obstacle = GetObstacleForBranch(side, branchIndex);
        if (obstacle == null)
        {
            Debug.LogWarning($"[BranchObstacleManager] No obstacle found for branch {branchIndex} in zone {side}.");
            return;
        }

        obstacle.Remove();
        obstaclesByZone[side].Remove(obstacle);
    }

    public void ClearAll()
    {
        foreach (Side side in obstaclesByZone.Keys)
            ClearContainer(GetContainer(side), side);
    }

    private void ClearContainer(Transform container, Side side)
    {
        if (container == null) return;

        if (obstaclesByZone.TryGetValue(side, out List<BranchObstacle> list))
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

    private Transform GetContainer(Side side) => side switch
    {
        Side.Top => topContainer,
        Side.Bottom => bottomContainer,
        Side.Left => leftContainer,
        Side.Right => rightContainer,
        _ => null,
    };
}
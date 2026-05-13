using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [SerializeField] private Transform topContainer;
    [SerializeField] private Transform bottomContainer;
    [SerializeField] private Transform rightContainer;
    [SerializeField] private Transform leftContainer;

    private void Awake()
    {
        ClearAll();
    }

    public void ClearAll()
    {
        ClearContainer(topContainer);
        ClearContainer(bottomContainer);
        ClearContainer(leftContainer);
        ClearContainer(rightContainer);
    }

    public void SpawnObstacles(ObstaclePlacer placer, ObstacleCatalogue catalogue, Vector2Int worldOffset, Side zone)
    {
        Transform container = GetContainer(zone);
        if(container == null)
        {
            Debug.LogWarning($"No container assigned for zone {zone}");
            return;
        }
        
        ClearContainer(container);

        foreach(ObstaclePlacementData data in placer.placedObstacles)
        {
            ObstacleDefinition def = catalogue.GetByName(data.definitionName);
            if(def == null || def.prefabVariants == null || def.prefabVariants.Count == 0)
                continue;

            GameObject prefab = def.prefabVariants[Random.Range(0, def.prefabVariants.Count)];
            if(prefab == null) continue;

            Vector3 worldPos = new Vector3(
                (worldOffset.x + data.rootTile.x + data.sizeX / 2f) * MasterManager.TileScale,
                0f,
                (worldOffset.y + data.rootTile.y + data.sizeZ / 2f) * MasterManager.TileScale
            );

            Quaternion rotation = (data.sizeX != def.sizeX)
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.identity;

            Instantiate(prefab, worldPos, rotation, container);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        for(int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (!Application.isEditor)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private Transform GetContainer(Side zone)
    {
        return zone switch
        {
            Side.Top => topContainer,
            Side.Bottom => bottomContainer,
            Side.Left => leftContainer,
            Side.Right => rightContainer,
            _ => null
        };
    }
}
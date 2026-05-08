using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleDefinition", menuName = "Map/Obstacle Definition")] 
public class ObstacleDefinition : ScriptableObject
{
    [Header("Identity")]
    public ObstacleType type;
    public string displayName;

    [Header("Placement")]
    public int sizeX;
    public int sizeZ;
    public float spawnWeight;

    [Header("Visuals")]
    public List<GameObject> prefabVariants;

    //Bellow is for future aditions mentioned in ObstacleType file
    [Header("Gameplay - import later")]
    public bool blocksMovement = true;
    public bool blocksProjectiles = false;
    public bool isDestructible = false;
}

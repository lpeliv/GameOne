using UnityEngine;

[CreateAssetMenu(fileName = "TurretDefinition", menuName = "Turrets/Turret Definition")]
public class TurretDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;

    [Header("Blueprint")]
    public string blueprintId;

    [Header("Prefabs")]
    public GameObject basePrefab;

    [Header("Health")]
    public float maxHealth = 200f;

    [Header("Cylinders")]
    public int cylinderCount = 2;
    public float rotationSpeed = 90f;

    [Header("Cost")]
    public int buildCost = 100;

    [Header("Construction")]
    public GameObject hitPointPrefab;
    public int hitsForLegs = 2;
    public int hitsForPlate = 2;

    [Header("Cylinder Construction")]
    public int hitsPerCylinder = 3;
}
using UnityEngine;

[CreateAssetMenu(fileName = "AddonDefinition", menuName = "Turrets/Addon Definition")]
public class AddonDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public AddonType addonType;

    [Header("Blueprint")]
    public string blueprintId;

    [Header("Prefab")]
    public GameObject addonPrefab;
    public GameObject projectilePrefab;

    [Header("Stats")]
    public float range = 20f;
    public float damage = 25f;
    public float fireRate = 1f;
    public float projectileSpeed = 15f;

    [Header("Upgrade Tiers")]
    public AddonUpgradeTier[] upgradeTiers;

    [Header("Cost")]
    public int buildCost = 50;
}
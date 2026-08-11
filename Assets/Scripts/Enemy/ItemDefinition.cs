using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public ItemRarity rarity;
    public Sprite icon;

    [Header("Prefab")]
    public GameObject worldPrefab;

    [Header("Inventory")]
    public int maxStack = 999;

    [Header("Auto Pickup")]
    public float pickupRadius = 3f;
    public float lifetimeDuration = 300f;
    [Range(0f, 1f)]
    public float autoPickupFee = 0.5f;

    [Header("Economy")]
    public int goldValue = 10;
}
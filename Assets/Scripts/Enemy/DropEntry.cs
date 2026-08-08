using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemDefinition item;
    public int quantity = 1;

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    [Range(0f, 1f)]
    public float dropChance;
}
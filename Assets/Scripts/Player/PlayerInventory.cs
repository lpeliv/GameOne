using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Starting Values")]
    [SerializeField] private int startingGold = 10500;

    private Dictionary<ItemDefinition, int> inventory = new Dictionary<ItemDefinition, int>();

    public static PlayerInventory Instance { get; private set; }

    private int gold = 0;
    public int Gold => gold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        gold = startingGold;
    }

    public void AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        if (!inventory.ContainsKey(item))
            inventory[item] = 0;

        int spaceLeft = item.maxStack - inventory[item];
        int actualAdd = Mathf.Min(quantity, spaceLeft);

        if (actualAdd <= 0)
        {
            Debug.LogWarning($"[PlayerInventory] {item.itemName} is at max stack ({item.maxStack}).");
            return;
        }

        inventory[item] += actualAdd;
        Debug.Log($"[PlayerInventory] Added {actualAdd}x {item.itemName}. Total: {inventory[item]}");
    }

    public bool RemoveItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        if (!inventory.ContainsKey(item) || inventory[item] < quantity)
        {
            Debug.LogWarning($"[PlayerInventory] Not enough {item.itemName}. " +
                             $"Have: {GetCount(item)}, Need: {quantity}");
            return false;
        }

        inventory[item] -= quantity;

        if (inventory[item] <= 0)
            inventory.Remove(item);

        Debug.Log($"[PlayerInventory] Removed {quantity}x {item.itemName}. " +
                  $"Remaining: {GetCount(item)}");
        return true;
    }

    public int GetCount(ItemDefinition item)
    {
        if (item == null) return 0;
        return inventory.TryGetValue(item, out int count) ? count : 0;
    }

    public bool HasItem(ItemDefinition item, int quantity = 1)
    {
        return GetCount(item) >= quantity;
    }

    public IReadOnlyDictionary<ItemDefinition, int> GetInventory() => inventory;

    public void PrintInventory()
    {
        Debug.Log("[PlayerInventory] Current inventory:");
        foreach (var kvp in inventory)
            Debug.Log($"  {kvp.Key.itemName}: {kvp.Value}");
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        Debug.Log($"[PlayerInventory] Gold added: {amount}. Total: {gold}");
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (gold < amount)
        {
            Debug.LogWarning($"[PlayerInventory] Not enough gold. Have: {gold}, Need: {amount}");
            return false;
        }
        gold -= amount;
        Debug.Log($"[PlayerInventory] Gold spent: {amount}. Remaining: {gold}");
        return true;
    }

    public bool HasGold(int amount) => gold >= amount;
}
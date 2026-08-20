using System.Collections.Generic;
using UnityEngine;

public class HammerUpgradeManager : MonoBehaviour
{
    [Header("Upgrade Definitions")]
    [SerializeField] private HammerUpgradeDefinition damageUpgrade;
    [SerializeField] private HammerUpgradeDefinition reachUpgrade;
    [SerializeField] private HammerUpgradeDefinition swingSpeedUpgrade;
    [SerializeField] private HammerUpgradeDefinition turretRepairUpgrade;
    [SerializeField] private HammerUpgradeDefinition knockbackUpgrade;
    [SerializeField] private HammerUpgradeDefinition abilityCooldownUpgrade;

    [Header("Hammer Model")]
    [SerializeField] private Transform hammerRoot;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    private Dictionary<HammerUpgradeStat, int> currentLevels = new Dictionary<HammerUpgradeStat, int>();
    private Dictionary<HammerUpgradeStat, float> currentStats = new Dictionary<HammerUpgradeStat, float>();

    private int currentZone = 1;

    public static HammerUpgradeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeStats();
    }

    private void InitializeStats()
    {
        foreach (HammerUpgradeStat stat in System.Enum.GetValues(typeof(HammerUpgradeStat)))
        {
            currentLevels[stat] = 0;
            currentStats[stat] = 0f;
        }

        Debug.Log("[HammerUpgradeManager] Stats initialized.");
    }

    public int GetCurrentLevel(HammerUpgradeStat stat) => currentLevels.TryGetValue(stat, out int level) ? level : 0;
    public float GetCurrentStat(HammerUpgradeStat stat) => currentStats.TryGetValue(stat, out float val) ? val : 0f;
    public int GetMaxLevel(HammerUpgradeStat stat) => GetDefinition(stat)?.GetMaxLevel(currentZone) ?? 0;

    public void SetZone(int zone)
    {
        currentZone = Mathf.Clamp(zone, 1, 4);
        Debug.Log($"[HammerUpgradeManager] Zone set to {currentZone}. New max level: {currentZone * 25}");
    }

    private HammerUpgradeDefinition GetDefinition(HammerUpgradeStat stat)
    {
        return stat switch
        {
            HammerUpgradeStat.Damage => damageUpgrade,
            HammerUpgradeStat.Reach => reachUpgrade,
            HammerUpgradeStat.SwingSpeed => swingSpeedUpgrade,
            HammerUpgradeStat.TurretRepair => turretRepairUpgrade,
            HammerUpgradeStat.Knockback => knockbackUpgrade,
            HammerUpgradeStat.AbilityCooldown => abilityCooldownUpgrade,
            _ => null
        };
    }

    public bool CanUpgrade(HammerUpgradeStat stat)
    {
        HammerUpgradeDefinition def = GetDefinition(stat);
        if (def == null) return false;

        int currentLevel = GetCurrentLevel(stat);
        int maxLevel = def.GetMaxLevel(currentZone);
        int cost = def.GetGoldCost(currentLevel);

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[HammerUpgradeManager] {stat} is at max level for zone {currentZone}.");
            return false;
        }

        if (!PlayerInventory.Instance.HasGold(cost))
        {
            Debug.Log($"[HammerUpgradeManager] Not enough gold. Need: {cost}, Have: {PlayerInventory.Instance.Gold}");
            return false;
        }

        return true;
    }

    public bool TryUpgrade(HammerUpgradeStat stat)
    {
        if (!CanUpgrade(stat)) return false;

        HammerUpgradeDefinition def = GetDefinition(stat);
        int currentLevel = GetCurrentLevel(stat);
        int cost = def.GetGoldCost(currentLevel);
        int newLevel = currentLevel + 1;
        float statIncrease = def.GetStatIncrease(newLevel);

        PlayerInventory.Instance.SpendGold(cost);

        currentLevels[stat] = newLevel;
        currentStats[stat] += statIncrease;

        ApplyStatToPlayer(stat);

        CheckMilestones(stat, newLevel, def);

        Debug.Log($"[HammerUpgradeManager] {stat} upgraded to level {newLevel}. Stat: {currentStats[stat]}");
        return true;
    }

    private void ApplyStatToPlayer(HammerUpgradeStat stat)
    {
        PlayerController player = playerController;
        if (player == null) return;

        switch (stat)
        {
            case HammerUpgradeStat.Damage:
                player.meleeDamage += currentStats[stat];
                break;
            case HammerUpgradeStat.Reach:
                player.hammerRange += currentStats[stat];
                break;
            case HammerUpgradeStat.SwingSpeed:
                player.swingDuration = Mathf.Max(0.1f, player.swingDuration - currentStats[stat]);
                break;
        }
    }

    private void CheckMilestones(HammerUpgradeStat stat, int newLevel, HammerUpgradeDefinition def)
    {
        if (newLevel % 5 == 0)
            Debug.Log($"[HammerUpgradeManager] Minor milestone reached for {stat} at level {newLevel}.");

        if (def.IsMajorMilestone(newLevel))
        {
            int milestoneIndex = (newLevel / def.maxLevelPerZone) - 1;
            if (def.milestoneAssets != null && milestoneIndex < def.milestoneAssets.Length)
            {
                GameObject asset = def.milestoneAssets[milestoneIndex];
                if (asset != null)
                {
                    Instantiate(asset, hammerRoot);
                    Debug.Log($"[HammerUpgradeManager] Major milestone! Added asset to hammer at level {newLevel}.");
                }
            }
        }
    }
}
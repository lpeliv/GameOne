using UnityEngine;

[CreateAssetMenu(fileName = "HammerUpgradeDefinition", menuName = "Blacksmith/Hammer Upgrade Definition")]
public class HammerUpgradeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public HammerUpgradeStat stat;

    [Header("Levels")]
    public int maxLevelPerZone = 25;
    public int totalZones = 4;
    public float baseStatIncrease = 1f;
    public float milestoneMultiplier = 2f;

    [Header("Cost")]
    public int baseGoldCost = 50;
    public int goldCostPerLevel = 10;

    [Header("Visual Milestones")]
    public GameObject[] milestoneAssets;

    [Header("Description")]
    [TextArea]
    public string description;

    public int GetMaxLevel(int currentZone)
    {
        return maxLevelPerZone * Mathf.Clamp(currentZone, 1, totalZones);
    }

    public int GetGoldCost(int currentLevel)
    {
        return baseGoldCost + goldCostPerLevel * currentLevel;
    }

    public float GetStatIncrease(int newLevel)
    {
        bool isMilestone = newLevel % 5 == 0;
        return isMilestone ? baseStatIncrease * milestoneMultiplier : baseStatIncrease;
    }

    public bool IsMajorMilestone(int level)
    {
        return level % maxLevelPerZone == 0 && level > 0;
    }
}
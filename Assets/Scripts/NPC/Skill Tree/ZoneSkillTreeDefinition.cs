using UnityEngine;

[CreateAssetMenu(fileName = "ZoneSkillTreeDefinition",
                 menuName = "Blacksmith/Zone Skill Tree Definition")]
public class ZoneSkillTreeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string zoneName;
    public int zoneIndex;      // 1-4
    public int levelStart;     // 1, 26, 51, 76
    public int levelEnd;       // 25, 50, 75, 100

    [Header("Prefabs")]
    public GameObject trunkPrefab;
    public GameObject segmentPrefab;
    public GameObject milestonePrefab;

    [Header("Colors")]
    public Color litColor;
    public Color unlitColor;
    public Color lockedColor;
    public Color milestoneColor;

    [Header("Layout")]
    public float segmentLength = 1.5f;
    public float segmentGap = 0.2f;
    public float milestoneRadius = 0.8f;
    public float branchStartDist = 2f;
    public float maxCurveAmount = 0.5f;
}
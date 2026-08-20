using System.Collections;
using UnityEngine;

public class SkillTreeBranch : MonoBehaviour
{
    [Header("Spline Control Points")]
    [SerializeField] private Transform p0; // trunk exit
    [SerializeField] private Transform p1; // first bend
    [SerializeField] private Transform p2; // second bend
    [SerializeField] private Transform p3; // branch tip

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int splineResolution = 30;
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Milestones")]
    [SerializeField] private MilestoneNode[] milestones;

    [Header("Growth")]
    [SerializeField] private float growSpeed = 0.5f;

    [Header("Stat")]
    [SerializeField] private HammerUpgradeStat stat;
    [SerializeField] private Color branchColor = Color.green;

    private float growProgress = 0f;
    private bool isGrowing = false;
    private int unlockedMilestones = 0;

    public HammerUpgradeStat Stat => stat;
    public float GrowProgress => growProgress;

    private Vector3 EvaluateBezier(float t)
    {
        float u = 1f - t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t2 = t * t;
        float t3 = t2 * t;

        return u3 * p0.position +
               3f * u2 * t * p1.position +
               3f * u * t2 * p2.position +
               t3 * p3.position;
    }

    private void UpdateLine()
    {
        int pointCount = Mathf.Max(2, Mathf.RoundToInt(splineResolution * growProgress));
        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1) * growProgress;
            lineRenderer.SetPosition(i, EvaluateBezier(t));
        }
    }

    private void Start()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.5f;
        lineRenderer.startColor = branchColor;
        lineRenderer.endColor = branchColor * 0.6f;
        lineRenderer.positionCount = 0;

        float[] milestoneTs = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
        for (int i = 0; i < milestones.Length; i++)
        {
            if (milestones[i] == null) continue;
            milestones[i].transform.position = EvaluateBezier(milestoneTs[i]);
            milestones[i].Initialize(stat, i, branchColor);
            milestones[i].gameObject.SetActive(false);
        }

        SyncWithUpgradeLevel();
    }

    public void SyncWithUpgradeLevel()
    {
        int level = HammerUpgradeManager.Instance.GetCurrentLevel(stat);
        int maxLevel = HammerUpgradeManager.Instance.GetMaxLevel(stat);

        growProgress = level / 25f;
        UpdateLine();

        float[] milestoneTs = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
        for (int i = 0; i < milestones.Length; i++)
        {
            int milestoneLevel = (i + 1) * (maxLevel / 5);
            bool reached = level >= milestoneLevel;
            bool isNext = !reached && level >= i * (maxLevel / 5) && milestoneLevel <= maxLevel;
            bool locked = milestoneLevel > maxLevel || level >= maxLevel;

            milestones[i].gameObject.SetActive(reached || isNext);
            milestones[i].SetState(reached, isNext, locked);
        }
    }

    public void GrowToNextMilestone()
    {
        if (isGrowing) return;

        int currentLevel = HammerUpgradeManager.Instance.GetCurrentLevel(stat);
        int maxLevel = HammerUpgradeManager.Instance.GetMaxLevel(stat);

        float targetT = currentLevel / (float)maxLevel;

        if (targetT <= growProgress) return;

        StartCoroutine(GrowTo(targetT));
    }

    private IEnumerator GrowTo(float targetT)
    {
        isGrowing = true;
        int levelAtStart = HammerUpgradeManager.Instance.GetCurrentLevel(stat);
        Debug.Log($"[Branch] GrowTo started. TargetT: {targetT}, Level: {levelAtStart}");

        while (growProgress < targetT - 0.001f)
        {
            growProgress += growSpeed * Time.deltaTime;
            growProgress = Mathf.Min(growProgress, targetT);
            UpdateLine();
            CheckMilestoneReveal();
            yield return null;
        }

        growProgress = targetT;
        isGrowing = false;
        UpdateLine();
        SyncWithUpgradeLevel();
    }

    private void CheckMilestoneReveal()
    {
        float[] milestoneTs = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };

        for (int i = 0; i < milestones.Length; i++)
        {
            if (milestones[i] == null) continue;
            if (growProgress >= milestoneTs[i] && !milestones[i].gameObject.activeSelf)
            {
                milestones[i].gameObject.SetActive(true);
                milestones[i].SetState(true, false, false);
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) UpdateMilestonePositions();
        else UpdateLine();
    }

    private void UpdateMilestonePositions()
    {
        if (p0 == null || p1 == null || p2 == null || p3 == null) return;

        float[] milestoneTs = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
        for (int i = 0; i < milestones.Length; i++)
        {
            if (milestones[i] == null) continue;
            milestones[i].transform.position = EvaluateBezier(milestoneTs[i]);
        }
    }

    private void OnDrawGizmos()
    {
        if (p0 == null || p1 == null || p2 == null || p3 == null) return;

        Gizmos.color = Color.green;
        Vector3 prev = EvaluateBezier(0f);
        for (int i = 1; i <= 20; i++)
        {
            Vector3 next = EvaluateBezier(i / 20f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p0.position, 0.1f);
        Gizmos.DrawSphere(p3.position, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(p1.position, 0.08f);
        Gizmos.DrawSphere(p2.position, 0.08f);
        Gizmos.DrawLine(p0.position, p1.position);
        Gizmos.DrawLine(p3.position, p2.position);
    }
}
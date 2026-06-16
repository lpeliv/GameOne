using System;
using System.Collections.Generic;
using UnityEngine;

public class TurretBuildMinigame : MonoBehaviour
{
    [Header("Construction Phases")]
    [SerializeField] private List<BuildPhase> phases;
    [SerializeField] private GameObject constructionTape;

    private int currentPhase;
    private int hitsCompleted;
    private List<HitPoint> activeHitPoints = new List<HitPoint>();
    private bool isActive;
    private GameObject hitPointPrefabRef;
    private List<HitPoint> allSpawnedHitPoints = new List<HitPoint>();

    public event Action OnMinigameComplete;
    public bool IsActive => isActive;

    public void StartMinigame(GameObject hitPointPrefab)
    {
        hitPointPrefabRef = hitPointPrefab;
        currentPhase = 0;
        isActive = true;
        hitsCompleted = 0;

        foreach (BuildPhase phase in phases)
            phase.phaseObject?.SetActive(false);

        StartPhase(0);
    }

    public void ResetMinigame()
    {
        isActive = false;
        hitsCompleted = 0;
        currentPhase = 0;

        ClearActiveHitPoints();

        foreach (BuildPhase phase in phases)
            phase.phaseObject?.SetActive(false);
    }

    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= phases.Count)
        {
            CompleteMinigame();
            return;
        }

        BuildPhase phase = phases[phaseIndex];
        hitsCompleted = 0;

        phase.phaseObject?.SetActive(true);
        ClearActiveHitPoints();

        foreach (Transform nailPoint in phase.nailPoints)
        {
            if (nailPoint == null) continue;

            GameObject prefabToUse = phase.hitPointPrefabOverride != null
                ? phase.hitPointPrefabOverride
                : hitPointPrefabRef;

            if (prefabToUse == null)
            {
                Debug.LogWarning("[TurretBuildMinigame] No hit point prefab assigned.");
                continue;
            }

            GameObject go = Instantiate(prefabToUse, nailPoint.position, nailPoint.rotation);
            HitPoint hitPoint = go.GetComponent<HitPoint>();

            if (hitPoint == null)
            {
                Debug.LogWarning("[TurretBuildMinigame] HitPoint prefab missing HitPoint component.");
                Destroy(go);
                continue;
            }
            hitPoint.Initialize(activeHitPoints.Count, -nailPoint.up);

            hitPoint.OnHitPointStruck += HandleHitPointStruck;
            activeHitPoints.Add(hitPoint);
            allSpawnedHitPoints.Add(hitPoint);
        }

        Debug.Log($"[TurretBuildMinigame] Phase {phaseIndex} started with {activeHitPoints.Count} hit points.");
    }

    private void HandleHitPointStruck(HitPoint hitPoint)
    {
        hitPoint.OnHitPointStruck -= HandleHitPointStruck;
        activeHitPoints.Remove(hitPoint);
        hitsCompleted++;

        Debug.Log($"[TurretBuildMinigame] Hit {hitsCompleted}. Remaining: {activeHitPoints.Count}");

        if (activeHitPoints.Count == 0)
            AdvancePhase();
    }

    private void AdvancePhase()
    {
        currentPhase++;
        StartPhase(currentPhase);
    }

    private void CompleteMinigame()
    {
        isActive = false;
        ClearActiveHitPoints();

        foreach (HitPoint hp in allSpawnedHitPoints)
            if (hp != null)
                Destroy(hp.gameObject);

        allSpawnedHitPoints.Clear();

        if (constructionTape != null)
            Destroy(constructionTape);

        OnMinigameComplete?.Invoke();
    }

    private void ClearActiveHitPoints()
    {
        foreach (HitPoint hp in activeHitPoints)
            if (hp != null)
                Destroy(hp.gameObject);

        activeHitPoints.Clear();
    }
}
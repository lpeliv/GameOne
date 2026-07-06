using System.Collections.Generic;
using UnityEngine;

public class TurretBase : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private TurretDefinition definition;

    [Header("Cylinder Mounts")]
    [SerializeField] private List<Transform> cylinderMounts;

    [Header("Construction")]
    [SerializeField] private TurretBuildMinigame buildMinigame;

    private List<TurretCylinder> cylinders = new List<TurretCylinder>();
    private float currentHP;
    private TurretBuildState buildState;

    public TurretDefinition Definition => definition;
    public float CurrentHP => currentHP;
    public TurretBuildState BuildState => buildState;
    public bool IsBuilt => buildState == TurretBuildState.Built;
    public bool IsDestroyed => buildState == TurretBuildState.Destroyed;

    public void Initialize(TurretDefinition def)
    {
        Debug.Log($"[TurretBase] Initialize called. Def: {def != null}");
        definition = def;
        currentHP = def.maxHealth;
        buildState = TurretBuildState.Empty;

        SpawnCylinders();
        SetupConstruction();
    }

    private void SpawnCylinders()
    {
        if (cylinderMounts == null || cylinderMounts.Count == 0)
        {
            Debug.LogWarning("[TurretBase] No cylinder mounts assigned.");
            return;
        }

        int count = Mathf.Min(definition.cylinderCount, cylinderMounts.Count);

        for (int i = 0; i < count; i++)
        {
            Transform mount = cylinderMounts[i];

            TurretCylinder cylinder = mount.GetComponentInChildren<TurretCylinder>();
            if (cylinder == null)
            {
                Debug.LogWarning($"[TurretBase] No TurretCylinder found on mount {i}.");
                continue;
            }

            cylinder.Initialize(definition.rotationSpeed);
            cylinders.Add(cylinder);
        }

        Debug.Log($"[TurretBase] Initialized with {cylinders.Count} cylinders.");
    }

    private void SetupConstruction()
    {
        Debug.Log($"[TurretBase] SetupConstruction called. BuildMinigame: {buildMinigame != null}, HitPointPrefab: {definition.hitPointPrefab != null}");

        if (buildMinigame == null)
        {
            Debug.LogWarning("[TurretBase] No build minigame assigned.");
            return;
        }

        foreach (Transform mount in cylinderMounts)
            mount.gameObject.SetActive(false);

        foreach (TurretCylinder cylinder in cylinders)
            cylinder.Deactivate();

        buildMinigame.OnMinigameComplete += OnBaseConstructionComplete;
        buildMinigame.StartMinigame(definition.hitPointPrefab);
        SetBuildState(TurretBuildState.UnderConstruction);

        Debug.Log("[TurretBase] Construction started.");
    }

    public void SetBuildState(TurretBuildState state)
    {
        buildState = state;
    }

    private void OnBaseConstructionComplete()
    {
        buildMinigame.OnMinigameComplete -= OnBaseConstructionComplete;
        OnAllConstructionComplete();
    }

    private void OnAllConstructionComplete()
    {
        Debug.Log($"[TurretBase] OnAllConstructionComplete called. Cylinder count: {cylinders.Count}");

        foreach (TurretCylinder cylinder in cylinders)
            cylinder.Initialize(definition.rotationSpeed);

        SetBuildState(TurretBuildState.Built);
    }

    public void TakeDamage(float amount)
    {
        if (buildState == TurretBuildState.Destroyed) return;

        currentHP = Mathf.Max(0f, currentHP - amount);
        Debug.Log($"[TurretBase] Took {amount} damage. HP: {currentHP}/{definition.maxHealth}");

        if (currentHP <= 0f)
            OnDestroyed();
    }

    public void Repair(float amount)
    {
        if (buildState == TurretBuildState.Destroyed) return;

        currentHP = Mathf.Min(definition.maxHealth, currentHP + amount);
        Debug.Log($"[TurretBase] Repaired {amount} HP. HP: {currentHP}/{definition.maxHealth}");
    }

    private void OnDestroyed()
    {
        buildState = TurretBuildState.Destroyed;
        foreach (TurretCylinder cylinder in cylinders)
            cylinder.Deactivate();

        Debug.Log("[TurretBase] Turret destroyed.");
    }

    public TurretCylinder GetCylinder(int index)
    {
        if (index < 0 || index >= cylinders.Count) return null;
        return cylinders[index];
    }

    public IReadOnlyList<TurretCylinder> GetAllCylinders() => cylinders;
}
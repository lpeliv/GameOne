using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private ZoneDefinition zoneDefinition;
    [SerializeField] private ZoneGridManager zoneGrid;
    [SerializeField] private SpawnerManager spawnerManager;

    [Header("Enemy")]
    [SerializeField] private EnemyDefinition miniBossDefinition;

    // Wave state
    private int currentWaveIndex = 0;
    private bool waveActive = false;
    private bool obstacleRemovedThisCycle = false;

    // Pool state
    private List<EnemyDefinition> remainingPool = new List<EnemyDefinition>();
    private List<EnemyPathFollower> aliveEnemies = new List<EnemyPathFollower>();
    private float currentWeightLimit;
    private float waveElapsedTime;
    private float releaseTimer;
    private bool poolExhausted = false;
    private bool miniBossSpawned = false;
    private EnemyPathFollower currentMiniBoss;

    // Charge state
    private int halfChargeCount = 0;
    private int obstacleRemoverCount = 0;

    // Properties
    public bool WaveActive => waveActive;
    public int CurrentWaveIndex => currentWaveIndex;
    public int HalfChargeCount => halfChargeCount;
    public int ObstacleRemoverCount => obstacleRemoverCount;

    private List<EnemySpawner> activeSpawners = new List<EnemySpawner>();
    [SerializeField] private BranchObstacleManager branchObstacleManager;
    [SerializeField] private HealthBudManager healthBudManager;

    [Header("Testing")]
    [SerializeField] private KeyCode startWaveKey = KeyCode.L;

    [Header("Testing")]
    [SerializeField] private KeyCode removeObstacleKey = KeyCode.R;
    [SerializeField] private int testBranchIndex = 0;

    public bool CanStartNextWave() =>
        zoneDefinition.CanStartWave(currentWaveIndex, obstacleRemovedThisCycle);

    private void Awake()
    {
        currentWaveIndex = 0;
        waveActive = false;
    }

    private void UpdateActiveSpawners()
    {
        activeSpawners.Clear();

        int unlockedCount = zoneDefinition.GetUnlockedSpawnerCount(currentWaveIndex);
        IReadOnlyList<EnemySpawner> zoneSpawners = spawnerManager.GetSpawnerForZone(zoneDefinition.side);

        foreach (EnemySpawner spawner in zoneSpawners)
        {
            if (spawner.SpawnerType == SpawnerType.Main)
            {
                activeSpawners.Add(spawner);
                break;
            }
        }

        int branchesAdded = 0;
        foreach (EnemySpawner spawner in zoneSpawners)
        {
            if (activeSpawners.Count >= unlockedCount) break;
            if (spawner.SpawnerType != SpawnerType.Branch) continue;

            activeSpawners.Add(spawner);
            branchesAdded++;
        }

        Debug.Log($"[WaveManager] Active spawners: {activeSpawners.Count} for wave {currentWaveIndex + 1}.");
    }

    public void TryStartWave()
    {
        Debug.Log($"[WaveManager] TryStartWave called. " +
             $"ZoneDefinition: {zoneDefinition != null}, " +
             $"ZoneGrid: {zoneGrid != null}, " +
             $"EnemyPath: {zoneGrid?.enemyPath != null}, " +
             $"Spawners: {spawnerManager?.GetSpawnerForZone(zoneDefinition.side).Count}");

        if (waveActive)
        {
            Debug.LogWarning("[WaveManager] Wave already active.");
            return;
        }

        if (!CanStartNextWave())
        {
            Debug.LogWarning("[WaveManager] Cannot start wave — obstacle must be removed first.");
            return;
        }

        WaveDefinition wave = zoneDefinition.GetWave(currentWaveIndex);
        if (wave == null)
        {
            Debug.LogWarning("[WaveManager] WaveDefinition is null.");
            return;
        }

        Debug.Log($"[WaveManager] Pool size: {wave.BuildShuffledPool().Count}");
        Debug.Log($"[WaveManager] Active spawners: {activeSpawners.Count}");

        if (waveActive)
        {
            Debug.LogWarning("[WaveManager] Wave already active.");
            return;
        }

        if (!CanStartNextWave())
        {
            Debug.LogWarning("[WaveManager] Cannot start wave — obstacle must be removed first.");
            return;
        }

        //WaveDefinition wave = zoneDefinition.GetWave(currentWaveIndex);
        if (wave == null) return;

        remainingPool = wave.BuildShuffledPool();
        currentWeightLimit = zoneDefinition.GetStartingWeightLimit(currentWaveIndex);
        waveElapsedTime = 0f;
        releaseTimer = 0f;
        poolExhausted = false;
        miniBossSpawned = false;
        obstacleRemovedThisCycle = false;
        waveActive = true;

        UpdateActiveSpawners();

        Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} started.");
    }

    private void CleanDeadEnemies()
    {
        aliveEnemies.RemoveAll(e => e == null || e.IsDead);
    }

    private void CheckWaveEnd()
    {
        if (!poolExhausted) return;
        if (!miniBossSpawned) return;
        if (aliveEnemies.Count > 0) return;

        waveActive = false;
        OnWaveComplete();
    }

    private void OnWaveComplete()
    {
        Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} complete.");
        AwardHalfCharge();

        currentWaveIndex++;

        if (currentWaveIndex >= zoneDefinition.waves.Count)
        {
            OnZoneComplete();
            return;
        }

        if (zoneDefinition.IsSpawnerUnlockWave(currentWaveIndex))
            Debug.Log("[WaveManager] Obstacle remover required before next wave can start.");
    }

    private void OnZoneComplete()
    {
        Debug.Log($"[WaveManager] Zone {zoneDefinition.zoneName} complete.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(startWaveKey) && !waveActive)
            TryStartWave();

        if (Input.GetKeyDown(removeObstacleKey))
            TryUseObstacleRemover(testBranchIndex);

        if (!waveActive) return;

        waveElapsedTime += Time.deltaTime;
        releaseTimer += Time.deltaTime;
        currentWeightLimit = zoneDefinition.GetWave(currentWaveIndex)
                                .GetWeightLimit(waveElapsedTime);

        CleanDeadEnemies();

        if (!poolExhausted)
            TryReleaseNext();
        else
            TrySpawnMiniBoss();

        CheckWaveEnd();
    }

    private float GetCurrentWeight()
    {
        float total = 0f;
        foreach (EnemyPathFollower enemy in aliveEnemies)
            if (enemy != null)
                total += enemy.Weight;
        return total;
    }

    private EnemySpawner GetRandomActiveSpawner()
    {
        if (activeSpawners.Count == 0)
        {
            Debug.LogWarning("[WaveManager] No active spawners.");
            return null;
        }

        return activeSpawners[Random.Range(0, activeSpawners.Count)];
    }

    private void TryReleaseNext()
    {
        if (remainingPool.Count == 0)
        {
            poolExhausted = true;
            return;
        }

        if (releaseTimer < zoneDefinition.GetWave(currentWaveIndex).releaseDelay)
            return;

        if (GetCurrentWeight() >= currentWeightLimit)
            return;

        EnemyDefinition definition = remainingPool[0];
        remainingPool.RemoveAt(0);

        EnemySpawner spawner = GetRandomActiveSpawner();
        if (spawner == null)
        {
            Debug.LogWarning("[WaveManager] No active spawner available.");
            return;
        }

        SpawnEnemy(definition, spawner);
        releaseTimer = 0f;
    }

    private void TrySpawnMiniBoss()
    {
        if (miniBossSpawned) return;
        if (aliveEnemies.Count > 0) return;

        WaveDefinition wave = zoneDefinition.GetWave(currentWaveIndex);

        EnemyDefinition bossDefinition = wave.isFinalWave
            ? wave.finalBossDefinition
            : wave.miniBossDefinition;

        if (bossDefinition == null)
        {
            Debug.LogWarning("[WaveManager] No boss definition assigned.");
            miniBossSpawned = true;
            return;
        }

        EnemySpawner spawner = GetRandomActiveSpawner();
        if (spawner == null) return;

        SpawnEnemy(bossDefinition, spawner, wave.isFinalWave
            ? wave.finalBossStatMultiplier
            : wave.miniBossStatMultiplier);

        miniBossSpawned = true;
        Debug.Log($"[WaveManager] {(wave.isFinalWave ? "Final boss" : "Mini boss")} spawned.");
        currentMiniBoss = aliveEnemies[aliveEnemies.Count - 1];
    }

    private void SpawnEnemy(EnemyDefinition definition, EnemySpawner spawner, float statMultiplier = 1f)
    {
        if (definition.prefabVariants == null || definition.prefabVariants.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] No prefab variants on {definition.displayName}.");
            return;
        }

        GameObject prefab = definition.prefabVariants[Random.Range(0, definition.prefabVariants.Count)];
        GameObject go = Instantiate(prefab, spawner.Data.worldPos, Quaternion.identity);

        EnemyPathFollower follower = go.GetComponent<EnemyPathFollower>();
        if (follower == null)
        {
            Debug.LogWarning("[WaveManager] Prefab missing EnemyPathFollower.");
            Destroy(go);
            return;
        }

        follower.SetPath(zoneGrid.enemyPath);
        follower.Initialize(definition, statMultiplier);
        follower.SetHealthBudManager(healthBudManager);
        follower.StartMoving();

        aliveEnemies.Add(follower);

        EnemyHealth health = go.GetComponent<EnemyHealth>();
        if (health == null)
        {
            Debug.LogWarning("[WaveManager] Enemy prefab missing EnemyHealth component.");
            return;
        }

        health.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(EnemyHealth health)
    {
        health.OnDeath -= HandleEnemyDeath;

        EnemyPathFollower follower = health.GetComponent<EnemyPathFollower>();
        if (follower != null)
        {
            follower.Die();
            aliveEnemies.Remove(follower);
        }

        if (health.GetComponent<EnemyPathFollower>() == currentMiniBoss)
        {
            currentMiniBoss = null;
            AwardHalfCharge();
            Debug.Log("[WaveManager] Mini boss killed, half charge awarded.");
        }

        Debug.Log($"[WaveManager] Enemy died. Alive: {aliveEnemies.Count}");
    }

    public void AwardHalfCharge()
    {
        halfChargeCount++;
        Debug.Log($"[WaveManager] Half charge awarded. Total: {halfChargeCount}");

        if (halfChargeCount >= 2)
        {
            halfChargeCount = 0;
            obstacleRemoverCount++;
            Debug.Log($"[WaveManager] Obstacle remover gained. Total: {obstacleRemoverCount}");
        }
    }

    public bool TryUseObstacleRemover(int branchIndex)
    {
        if (obstacleRemoverCount <= 0)
        {
            Debug.LogWarning("[WaveManager] No obstacle removers available.");
            return false;
        }

        BranchObstacle obstacle = branchObstacleManager.GetObstacleForBranch(zoneDefinition.side, branchIndex);
        if (obstacle == null)
        {
            Debug.LogWarning($"[WaveManager] No obstacle found for branch {branchIndex}.");
            return false;
        }

        obstacleRemoverCount--;
        obstacleRemovedThisCycle = true;
        branchObstacleManager.RemoveObstacleForBranch(zoneDefinition.side, branchIndex);

        Debug.Log($"[WaveManager] Branch {branchIndex} obstacle removed. Remainig removers: {obstacleRemoverCount}");
        return true;
    }
}
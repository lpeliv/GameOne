using System.Collections;
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

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("NPCs")]
    [SerializeField] private List<NPCBase> npcs;

    [Header("Doors")]
    [SerializeField] private List<ZoneDoor> zoneDoors;

    private int currentWaveIndex = 0;
    private bool waveActive = false;
    private bool obstacleRemovedThisCycle = false;

    private List<EnemyDefinition> remainingPool = new List<EnemyDefinition>();
    private List<EnemyPathFollower> aliveEnemies = new List<EnemyPathFollower>();
    private float currentWeightLimit;
    private float waveElapsedTime;
    private float releaseTimer;
    private bool poolExhausted = false;
    private bool miniBossSpawned = false;
    private EnemyPathFollower currentMiniBoss;

    private int halfChargeCount = 0;
    private int obstacleRemoverCount = 0;

    public static WaveManager Instance { get; private set; }

    public bool WaveActive => waveActive;
    public int CurrentWaveIndex => currentWaveIndex;
    public int HalfChargeCount => halfChargeCount;
    public int ObstacleRemoverCount => obstacleRemoverCount;

    private List<EnemySpawner> activeSpawners = new List<EnemySpawner>();
    private int lastSpawnerIndex = 0;
    private Dictionary<Vector2Int, EnemyPath> spawnerPathCache = new Dictionary<Vector2Int, EnemyPath>();
    [SerializeField] private BranchObstacleManager branchObstacleManager;
    [SerializeField] private HealthBudManager healthBudManager;

    private WaveSnapshot currentSnapshot;

    [Header("Testing")]
    [SerializeField] private KeyCode startWaveKey = KeyCode.L;
    [SerializeField] private KeyCode endWaveKey = KeyCode.K;

    private bool isGameOver = false;

    public bool CanStartNextWave() =>
        zoneDefinition.CanStartWave(currentWaveIndex, obstacleRemovedThisCycle);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentWaveIndex = 0;
        waveActive = false;
    }

    private void UpdateActiveSpawners()
    {
        activeSpawners.Clear();
        lastSpawnerIndex = 0;
        spawnerPathCache.Clear();

        IReadOnlyList<EnemySpawner> zoneSpawners = spawnerManager.GetSpawnerForZone(zoneDefinition.side);
        Side side = zoneDefinition.side;

        Debug.Log($"[WaveManager] UpdateActiveSpawners: zoneSpawners={zoneSpawners.Count}");

        foreach (EnemySpawner spawner in zoneSpawners)
        {
            if (spawner.SpawnerType == SpawnerType.Main)
            {
                activeSpawners.Add(spawner);
                Debug.Log($"[WaveManager] Added Main spawner: {spawner.name}, GridPos={spawner.Data.gridPos}, IsActive={spawner.IsActive}");
                break;
            }
        }

        int branchIndex = 0;
        foreach (EnemySpawner spawner in zoneSpawners)
        {
            if (spawner.SpawnerType != SpawnerType.Branch) continue;

            bool obstacleRemoved = branchObstacleManager.IsObstacleRemoved(side, branchIndex);
            Debug.Log($"[WaveManager] Branch spawner: {spawner.name}, branchIndex={branchIndex}, GridPos={spawner.Data.gridPos}, obstacleRemoved={obstacleRemoved}");

            if (obstacleRemoved)
            {
                activeSpawners.Add(spawner);
                Debug.Log($"[WaveManager] Added Branch spawner: {spawner.name}, IsActive={spawner.IsActive}");
            }
            else
            {
                Debug.Log($"[WaveManager] Skipped Branch spawner: {spawner.name} (obstacle still present)");
            }

            branchIndex++;
        }

        Debug.Log($"[WaveManager] Active spawners: {activeSpawners.Count} for wave {currentWaveIndex + 1}.");
    }

    private Coroutine waveStartCoroutine;

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

        TakeSnapshot();

        WaveDefinition wave = zoneDefinition.GetWave(currentWaveIndex);
        if (wave == null)
        {
            Debug.LogWarning("[WaveManager] WaveDefinition is null.");
            return;
        }

        SendNPCsToHouse();

        if (waveStartCoroutine != null)
            StopCoroutine(waveStartCoroutine);
        waveStartCoroutine = StartCoroutine(WaitUntilNPCsInsideThenStart(wave));
    }

    private IEnumerator WaitUntilNPCsInsideThenStart(WaveDefinition wave)
    {
        Debug.Log("[WaveManager] Waiting for NPCs to reach house...");

        bool allInside = false;
        while (!allInside)
        {
            allInside = true;
            foreach (NPCBase npc in npcs)
            {
                if (npc != null && !npc.IsAtHouse)
                {
                    allInside = false;
                    break;
                }
            }
            if (!allInside)
                yield return null;
        }

        Debug.Log("[WaveManager] All NPCs inside. Closing doors and starting wave.");
        CloseDoors();

        remainingPool = wave.BuildShuffledPool();
        currentWeightLimit = zoneDefinition.GetStartingWeightLimit(currentWaveIndex);
        waveElapsedTime = 0f;
        releaseTimer = 0f;
        poolExhausted = false;
        miniBossSpawned = false;
        obstacleRemovedThisCycle = false;
        waveActive = true;

        UpdateActiveSpawners();
        ActivateSpawners();

        Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} started.");
        waveStartCoroutine = null;
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

        currentWaveIndex++;

        if (currentWaveIndex >= zoneDefinition.waves.Count)
        {
            OnZoneComplete();
            return;
        }

        if (zoneDefinition.IsSpawnerUnlockWave(currentWaveIndex))
            Debug.Log("[WaveManager] Obstacle remover required before next wave can start.");

        OpenDoors();
        SendNPCsToOutpost();
    }

    private void OnZoneComplete()
    {
        Debug.Log($"[WaveManager] Zone complete.");
        OpenDoors();
        SendNPCsToOutpost();
        GameProgressionManager.Instance?.OnZoneComplete();
    }

    private void Update()
    {
        if (isGameOver) return;

        if (Input.GetKeyDown(startWaveKey) && !waveActive)
            TryStartWave();

        if (Input.GetKeyDown(endWaveKey) && waveActive)
            ForceEndWave();

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

    private EnemyPath GetPathForSpawner(EnemySpawner spawner)
    {
        Vector2Int gridPos = spawner.Data.gridPos;

        if (spawner.SpawnerType == SpawnerType.Main)
            return zoneGrid.enemyPath;

        if (spawnerPathCache.TryGetValue(gridPos, out EnemyPath cached))
            return cached;

        EnemyPath path = zoneGrid.BuildPathFromSpawner(gridPos);
        spawnerPathCache[gridPos] = path;
        Debug.Log($"[WaveManager] Built path for branch spawner at {gridPos}, waypoints: {path.Count}");
        Debug.Log($"[WaveManager] Path built for {spawner.name}: {path?.Count} waypoints");
        return path;
    }

    private void ActivateSpawners()
    {
        foreach (EnemySpawner spawner in activeSpawners)
        {
            if (spawner != null && !spawner.IsActive)
                spawner.Activate();
        }
    }

    private EnemySpawner GetNextSpawner()
    {
        if (activeSpawners.Count == 0)
        {
            Debug.LogWarning("[WaveManager] No active spawners.");
            return null;
        }

        int attempts = activeSpawners.Count;
        while (attempts > 0)
        {
            int index = lastSpawnerIndex % activeSpawners.Count;
            lastSpawnerIndex++;
            EnemySpawner spawner = activeSpawners[index];
            if (spawner != null && spawner.IsActive)
            {
                Debug.Log($"[WaveManager] Spawning from: {spawner.name}");
                return spawner;
            }
            attempts--;
        }

        Debug.LogWarning("[WaveManager] No active spawners available.");
        return null;
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

        EnemySpawner spawner = GetNextSpawner();
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

        EnemySpawner spawner = GetNextSpawner();
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

        Debug.Log($"[WaveManager] Spawning from: {spawner.name}, GridPos: {spawner.Data.gridPos}, Type: {spawner.SpawnerType}");
        EnemyPath path = GetPathForSpawner(spawner);
        follower.SetPath(path);
        follower.Initialize(definition, statMultiplier);
        follower.SetHealthBudManager(healthBudManager);
        follower.StartMoving();

        aliveEnemies.Add(follower);
        EnemyRegistry.Instance.Register(follower);

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
        Debug.Log($"[WaveManager] TryUseObstacleRemover called with branchIndex: {branchIndex}");

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

        Debug.Log($"[WaveManager] Found obstacle at branchIndex: {branchIndex}, GridPos: {obstacle.GridPos}");

        obstacleRemoverCount--;
        obstacleRemovedThisCycle = true;
        branchObstacleManager.RemoveObstacleForBranch(zoneDefinition.side, branchIndex);

        UpdateActiveSpawners();
        ActivateSpawners();

        Debug.Log($"[WaveManager] Branch {branchIndex} obstacle removed. Remaining removers: {obstacleRemoverCount}");
        Debug.Log($"[WaveManager] Active spawners after removal:");
        for (int i = 0; i < activeSpawners.Count; i++)
            Debug.Log($"  [{i}] {activeSpawners[i].name}, GridPos={activeSpawners[i].Data.gridPos}, IsActive={activeSpawners[i].IsActive}");

        return true;
    }

    public void ForceEndWave()
    {
        if (!waveActive) return;

        foreach (EnemyPathFollower enemy in aliveEnemies.ToArray())
            if (enemy != null)
                Destroy(enemy.gameObject);

        aliveEnemies.Clear();
        remainingPool.Clear();
        poolExhausted = true;
        miniBossSpawned = true;
        waveActive = false;

        waveActive = false;
        OnWaveComplete();

        Debug.Log("[WaveManager] Wave force ended.");
    }

    public void TakeSnapshot()
    {
        currentSnapshot = new WaveSnapshot();
        currentSnapshot.waveIndex = currentWaveIndex;
        currentSnapshot.playerHP = PlayerHealth.Instance?.CurrentHP ?? 0f;
        currentSnapshot.budSnapshots = new List<BudSnapshot>();

        IReadOnlyList<HealthBud> buds = healthBudManager.GetAllBuds();
        for (int i = 0; i < buds.Count; i++)
        {
            BudSnapshot budSnap = new BudSnapshot();
            budSnap.budIndex = i;
            budSnap.currentHP = buds[i].CurrentHealth;
            budSnap.isDestroyed = buds[i].IsDestroyed;
            currentSnapshot.budSnapshots.Add(budSnap);
        }

        Debug.Log($"[WaveManager] Snapshot taken. Wave: {currentWaveIndex}, PlayerHP: {currentSnapshot.playerHP}, Buds: {currentSnapshot.budSnapshots.Count}");
    }

    public void RestoreSnapshot()
    {
        if (currentSnapshot == null)
        {
            Debug.LogWarning("[WaveManager] No snapshot to restore.");
            return;
        }

        IReadOnlyList<HealthBud> buds = healthBudManager.GetAllBuds();
        foreach (BudSnapshot budSnap in currentSnapshot.budSnapshots)
        {
            if (budSnap.budIndex >= buds.Count) continue;

            HealthBud bud = buds[budSnap.budIndex];
            if (budSnap.isDestroyed)
            {
                bud.SetDestroyed();
            }
            else
            {
                bud.Restore();
                bud.SetHealth(budSnap.currentHP);
            }
        }

        PlayerHealth.Instance?.SetHP(currentSnapshot.playerHP);

        Debug.Log($"[WaveManager] Snapshot restored. Wave: {currentSnapshot.waveIndex}");
    }

    public void RestartWave()
    {
        isGameOver = false;
        waveActive = false;

        List<EnemyPathFollower> enemiesToDespawn = new List<EnemyPathFollower>(aliveEnemies);
        foreach (EnemyPathFollower enemy in enemiesToDespawn)
        {
            if (enemy == null) continue;
            EnemyRegistry.Instance?.Unregister(enemy);
            Destroy(enemy.gameObject);
        }

        aliveEnemies.Clear();
        remainingPool.Clear();
        poolExhausted = false;
        miniBossSpawned = false;
        releaseTimer = 0f;
        waveElapsedTime = 0f;
        
        ClearWorldDrops();
        RestoreSnapshot();
        healthBudManager.ResubscribeBuds();
        playerHealth?.ResetForZone(healthBudManager.Zone, healthBudManager.AliveBudCount());
        currentWaveIndex = currentSnapshot.waveIndex;

        OpenDoors();
        SendNPCsToOutpost();
        Debug.Log($"[WaveManager] Wave restarted. Wave: {currentWaveIndex}");
    }

    private void ClearWorldDrops()
    {
        WorldDrop[] drops = FindObjectsByType<WorldDrop>(FindObjectsSortMode.None);
        foreach (WorldDrop drop in drops)
            Destroy(drop.gameObject);
    }

    public void SetGameOver(bool state)
    {
        isGameOver = state;
    }

    private void SendNPCsToHouse()
    {
        foreach (NPCBase npc in npcs)
            if (npc != null)
                npc.WalkToHouse();
    }

    private void SendNPCsToOutpost()
    {
        foreach (NPCBase npc in npcs)
            if (npc != null)
                npc.WalkToOutpost();
    }

    private void OpenDoors()
    {
        foreach (ZoneDoor door in zoneDoors)
        {
            if (door != null && door.State != ZoneDoorState.Locked)
                door.SetState(ZoneDoorState.Open);
        }
    }

    private void CloseDoors()
    {
        foreach (ZoneDoor door in zoneDoors)
        {
            if (door != null && door.State != ZoneDoorState.Locked)
                door.SetState(ZoneDoorState.Closed);
        }
    }
}
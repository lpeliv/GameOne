using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveDefinition", menuName = "Waves/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Enemy Pool")]
    public List<WaveEnemyEntry> enemyPool;

    [Header("Weight")]
    public float startingWeightLimit = 5f;
    public float weightLimitIncreaseRate = 0.5f;

    [Header("Release")]
    public float releaseDelay = 3f;

    [Header("Mini Boss")]
    public EnemyDefinition miniBossDefinition;
    public float miniBossStatMultiplier = 3f;

    [Header("Final Wave")]
    public bool isFinalWave = false;
    public EnemyDefinition finalBossDefinition;
    public float finalBossStatMultiplier = 5f;

    public List<EnemyDefinition> BuildShuffledPool()
    {
        List<EnemyDefinition> pool = new List<EnemyDefinition>();

        foreach (WaveEnemyEntry entry in enemyPool)
            for (int i = 0; i < entry.count; i++)
                pool.Add(entry.definition);

        ShuffleList(pool);
        return pool;
    }

    public float GetWeightLimit(float elapsedTime)
    {
        return startingWeightLimit + weightLimitIncreaseRate * elapsedTime;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
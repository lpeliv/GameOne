using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZoneDefinition", menuName = "Waves/Zone Definition")]
public class ZoneDefinition : ScriptableObject
{
    [Header("Identity")]
    public string zoneName;
    public Side side;

    [Header("Waves")]
    public List<WaveDefinition> waves;

    [Header("Spawner Unlock Schedule")]
    public int wavesPerSpawnerUnlock = 2;

    [Header("Weight Progression")]
    public float baseWeightLimitIncreasePerWave = 1f;

    public WaveDefinition GetWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogWarning($"[ZoneDefinition] Wave index {waveIndex} out of range for zone {zoneName}.");
            return null;
        }
        return waves[waveIndex];
    }

    public int GetUnlockedSpawnerCount(int waveIndex)
    {
        return Mathf.FloorToInt(waveIndex / wavesPerSpawnerUnlock) + 1;
    }

    public bool IsSpawnerUnlockWave(int waveIndex)
    {
        return waveIndex > 0 && waveIndex % wavesPerSpawnerUnlock == 0;
    }

    public bool IsFinalWave(int waveIndex)
    {
        return waveIndex == waves.Count - 1;
    }

    public float GetStartingWeightLimit(int waveIndex)
    {
        return waves[waveIndex].startingWeightLimit +
               baseWeightLimitIncreasePerWave * waveIndex;
    }

    public bool CanStartWave(int waveIndex, bool obstacleRemovedThisCycle)
    {
        if (waveIndex == 0) return true;
        if (IsSpawnerUnlockWave(waveIndex) && !obstacleRemovedThisCycle)
            return false;
        return true;
    }
}
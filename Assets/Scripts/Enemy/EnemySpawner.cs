using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private SpawnerData data;
    private bool isActive;

    public SpawnerData Data => data;
    public bool IsActive => isActive;
    public Side Zone => data.zone;
    public SpawnerType SpawnerType => data.spawnerType;

    public void Initialize(SpawnerData spawnerData)
    {
        data = spawnerData;
        isActive = false;
    }

    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        OnActivated();
    }

    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        OnDeactivated();
    }

    protected virtual void OnActivated()
    {

    }

    protected virtual void OnDeactivated()
    {

    }
}

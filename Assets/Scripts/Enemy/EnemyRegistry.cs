using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    public static EnemyRegistry Instance { get; private set; }

    private readonly List<EnemyPathFollower> activeEnemies = new List<EnemyPathFollower>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(EnemyPathFollower enemy)
    {
        if (enemy == null || activeEnemies.Contains(enemy)) return;
        activeEnemies.Add(enemy);
    }

    public void Unregister(EnemyPathFollower enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public EnemyPathFollower GetClosestEnemy(Vector3 position, float range)
    {
        EnemyPathFollower closest = null;
        float minDist = float.MaxValue;

        foreach (EnemyPathFollower enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    public EnemyPathFollower GetFirstInLine(Vector3 position, float range)
    {
        EnemyPathFollower first = null;
        int maxIndex = -1;

        foreach (EnemyPathFollower enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist > range) continue;

            if (enemy.WaypointIndex > maxIndex)
            {
                maxIndex = enemy.WaypointIndex;
                first = enemy;
            }
        }

        return first;
    }

    public EnemyPathFollower GetHighestHP(Vector3 position, float range)
    {
        EnemyPathFollower target = null;
        float maxHP = float.MinValue;

        foreach (EnemyPathFollower enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist > range) continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) continue;

            if (health.CurrentHealth > maxHP)
            {
                maxHP = health.CurrentHealth;
                target = enemy;
            }
        }

        return target;
    }

    public EnemyPathFollower GetLowestHP(Vector3 position, float range)
    {
        EnemyPathFollower target = null;
        float minHP = float.MaxValue;

        foreach (EnemyPathFollower enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist > range) continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) continue;

            if (health.CurrentHealth < minHP)
            {
                minHP = health.CurrentHealth;
                target = enemy;
            }
        }

        return target;
    }

    public void Clear()
    {
        activeEnemies.Clear();
    }
}
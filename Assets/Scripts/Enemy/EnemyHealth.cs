using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private float maxHealth;
    private float currentHealth;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public event Action<EnemyHealth> OnDeath;
    public event Action<EnemyHealth, float> OnDamageTaken;

    public void Initialize(EnemyDefinition definition, float rolledSize, float statMultiplier)
    {
        maxHealth = definition.baseHealth * rolledSize * statMultiplier;
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"[EnemyHealth] TakeDamage called. Amount: {amount}, CurrentHealth before: {currentHealth}, IsDead: {isDead}");

        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamageTaken?.Invoke(this, amount);

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        SpawnDrops();
        OnDeath?.Invoke(this);
    }

    private void SpawnDrops()
    {
        EnemyDefinition definition = GetComponent<EnemyPathFollower>()?.Definition;
        if (definition == null) return;

        List<DropEntry> drops = definition.dropTable.RollDrops();

        foreach (DropEntry drop in drops)
        {
            if (drop.item?.worldPrefab == null) continue;

            for (int i = 0; i < drop.quantity; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0f,
                UnityEngine.Random.Range(-1f, 1f)
            );

                GameObject go = Instantiate(drop.item.worldPrefab, spawnPos, Quaternion.identity);
                WorldDrop world = go.GetComponent<WorldDrop>();

                if (world == null)
                {
                    Debug.LogWarning("[EnemyHealth] WorldDrop component missing on item prefab.");
                    continue;
                }

                world.Initialize(drop.item, 1);
            }
        }
    }
}
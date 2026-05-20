using System;
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

        OnDeath?.Invoke(this);
    }
}
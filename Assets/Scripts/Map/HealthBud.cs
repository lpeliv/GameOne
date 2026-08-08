using System;
using UnityEngine;

public class HealthBud : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public Side zone;
    private float currentHealth;
    private bool isDestroyed;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDestroyed => isDestroyed;

    public event Action<HealthBud> OnBudDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
            DestroyBud();
    }

    private void DestroyBud()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        OnBudDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void Restore()
    {
        isDestroyed = false;
        currentHealth = maxHealth;
        gameObject.SetActive(true);
    }

    public void SetHealth(float hp)
    {
        currentHealth = Mathf.Clamp(hp, 0f, maxHealth);
    }

    public void SetDestroyed()
    {
        isDestroyed = true;
        currentHealth = 0f;
        gameObject.SetActive(false);
    }
}
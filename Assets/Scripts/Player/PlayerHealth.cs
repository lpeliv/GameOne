using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float hpPerBud = 100f;
    [SerializeField] private float invincibilityDuration = 1f;

    private float maxHP;
    private float currentHP;
    private bool isInvincible;
    private float invincibilityTimer;

    private Dictionary<Side, int> zoneActiveBuds = new Dictionary<Side, int>();
    private bool isFinalRound = false;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsAlive => currentHP > 0f;

    public static PlayerHealth Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetForZone(Side zone, int totalBudCount)
    {
        isFinalRound = false;
        zoneActiveBuds.Clear();
        zoneActiveBuds[zone] = totalBudCount;

        maxHP = zoneActiveBuds[zone] * hpPerBud;
        currentHP = maxHP;

        Debug.Log($"[PlayerHealth] Zone reset. BudsPerZone: {zoneActiveBuds[zone]}, MaxHP: {maxHP}");
    }

    public void AddZonePool(Side zone, int budCount)
    {
        isFinalRound = true;
        zoneActiveBuds[zone] = budCount;

        maxHP = CalculateTotalMaxHP();
        currentHP = Mathf.Min(currentHP, maxHP);

        Debug.Log($"[PlayerHealth] Added zone pool. Zone: {zone}, BudCount: {budCount}, TotalMaxHP: {maxHP}");
    }

    public void OnBudDestroyed(Side zone)
    {
        if (!zoneActiveBuds.ContainsKey(zone)) return;

        zoneActiveBuds[zone] = Mathf.Max(0, zoneActiveBuds[zone] - 1);

        if (isFinalRound)
            maxHP = CalculateTotalMaxHP();
        else
            maxHP = (zoneActiveBuds[zone] * hpPerBud);

        currentHP = Mathf.Min(currentHP, maxHP);

        Debug.Log($"[PlayerHealth] Bud destroyed. MaxHP: {maxHP}, CurrentHP: {currentHP}");

        if (maxHP <= 0f)
            OnDeath();
    }

    private void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer += Time.deltaTime;
            if (invincibilityTimer >= invincibilityDuration)
            {
                isInvincible = false;
                invincibilityTimer = 0f;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        if (isInvincible) return;

        currentHP = Mathf.Max(0f, currentHP - amount);
        isInvincible = true;
        invincibilityTimer = 0f;

        Debug.Log($"[PlayerHealth] Took {amount} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            OnDeath();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHP}/{maxHP}");
    }

    private float CalculateTotalMaxHP()
    {
        float total = 0f;
        foreach (int budCount in zoneActiveBuds.Values)
            total += budCount * hpPerBud;
        return total;
    }

    private void OnDeath()
    {
        Debug.Log("[PlayerHealth] Player died — game over.");
        // TODO: trigger game over UI, restart wave
    }

    public void SetHP(float hp)
    {
        currentHP = Mathf.Clamp(hp, 0f, maxHP);
    }
}
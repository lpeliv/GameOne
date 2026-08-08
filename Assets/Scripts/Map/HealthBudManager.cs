using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthBudManager : MonoBehaviour
{
    [SerializeField] private Side zone;

    private Side activeZone;
    public Side Zone => zone;
    public IReadOnlyList<HealthBud> GetAllBuds() => buds;

    private List<HealthBud> buds = new List<HealthBud>();

    public event Action OnAllBudsDestroyed;

    public void RegisterBuds(List<HealthBud> healthBuds)
    {
        buds.Clear();

        foreach (HealthBud bud in healthBuds)
        {
            if (bud == null) continue;
            buds.Add(bud);
            bud.OnBudDestroyed += HandleBudDestroyed;
        }
    }

    public HealthBud GetClosestBud(Vector3 position)
    {
        HealthBud closest = null;
        float minDist = float.MaxValue;

        foreach (HealthBud bud in buds)
        {
            if (bud == null || bud.IsDestroyed) continue;
            if (bud.zone != activeZone) continue;

            float dist = Vector3.Distance(position, bud.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = bud;
            }
        }

        return closest;
    }

    public bool AnyBudsAlive()
    {
        foreach (HealthBud bud in buds)
            if (bud != null && !bud.IsDestroyed && bud.zone == activeZone)
                return true;
        return false;
    }

    public int AliveBudCount()
    {
        int count = 0;
        foreach (HealthBud bud in buds)
            if (bud != null && !bud.IsDestroyed && bud.zone == activeZone)
                count++;
        return count;
    }

    private void HandleBudDestroyed(HealthBud bud)
    {
        bud.OnBudDestroyed -= HandleBudDestroyed;
        if (bud.zone != activeZone) return;
        PlayerHealth.Instance?.OnBudDestroyed(activeZone);
        Debug.Log($"[HealthBudManager] Active zone bud destroyed. Remaining: {AliveBudCount()}");

        if (!AnyBudsAlive())
        {
            Debug.Log("[HealthBudManager] All active zone buds destroyed.");
            OnAllBudsDestroyed?.Invoke();
        }
    }

    private Dictionary<HealthBud, List<Vector3>> claimedPositions =
    new Dictionary<HealthBud, List<Vector3>>();

    public Vector3 ClaimAttackPosition(HealthBud bud, float attackRange)
    {
        if (!claimedPositions.ContainsKey(bud))
            claimedPositions[bud] = new List<Vector3>();

        List<Vector3> claimed = claimedPositions[bud];

        int maxAttempts = 12;
        float angleStep = 360f / maxAttempts;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = i * angleStep;
            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * attackRange,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * attackRange
            );
            Vector3 candidate = bud.transform.position + offset;

            bool taken = false;
            foreach (Vector3 pos in claimed)
            {
                if (Vector3.Distance(candidate, pos) < attackRange * 0.5f)
                {
                    taken = true;
                    break;
                }
            }

            if (!taken)
            {
                claimed.Add(candidate);
                return candidate;
            }
        }

        float fallbackAngle = UnityEngine.Random.Range(0f, 360f);
        Vector3 fallback = bud.transform.position + new Vector3(
            Mathf.Sin(fallbackAngle * Mathf.Deg2Rad) * attackRange,
            0f,
            Mathf.Cos(fallbackAngle * Mathf.Deg2Rad) * attackRange
        );
        return fallback;
    }

    public void ReleaseAttackPosition(HealthBud bud, Vector3 position)
    {
        if (!claimedPositions.ContainsKey(bud)) return;
        claimedPositions[bud].Remove(position);
    }

    public void ClearClaimedPositions(HealthBud bud)
    {
        if (claimedPositions.ContainsKey(bud))
            claimedPositions[bud].Clear();
    }

    public void SetActiveZone(Side zone)
    {
        activeZone = zone;
    }

    // Testing - should delete later

    [Header("Testing")]
    [SerializeField] private KeyCode destroyBudKey = KeyCode.Z;

    private void Update()
    {
        if (Input.GetKeyDown(destroyBudKey))
            DestroyRandomBud();
    }

    private void DestroyRandomBud()
    {
        foreach (HealthBud bud in buds)
        {
            if (bud != null && !bud.IsDestroyed)
            {
                bud.TakeDamage(bud.MaxHealth);
                Debug.Log($"[HealthBudManager] Test destroyed a bud. Remaining: {AliveBudCount()}");
                return;
            }
        }

        Debug.LogWarning("[HealthBudManager] No buds left to destroy.");
    }

    // End testing 

    public void ResubscribeBuds()
    {
        foreach (HealthBud bud in buds)
        {
            if (bud == null) continue;
            bud.OnBudDestroyed -= HandleBudDestroyed;
            bud.OnBudDestroyed += HandleBudDestroyed;
        }
    }
}
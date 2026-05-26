using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private float damageAmount = 50f;
    [SerializeField] private KeyCode damageKey = KeyCode.F;
    [SerializeField] private KeyCode budDamageKey = KeyCode.G;

    private EnemyHealth targetHealth;
    private HealthBud targetBud;

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            targetHealth = health;
            return;
        }

        HealthBud bud = other.GetComponent<HealthBud>();
        if (bud != null)
            targetBud = bud;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<EnemyHealth>() == targetHealth)
            targetHealth = null;

        if (other.GetComponent<HealthBud>() == targetBud)
            targetBud = null;
    }

    private void Update()
    {
        if (targetBud != null && (!targetBud.gameObject.activeInHierarchy || targetBud.IsDestroyed))
        {
            targetBud = null;
            Debug.Log("[DamageDealer] Target bud cleared.");
        }

        if (targetHealth != null && !targetHealth.gameObject.activeInHierarchy)
            targetHealth = null;

        if (Input.GetKeyDown(damageKey) && targetHealth != null)
        {
            targetHealth.TakeDamage(damageAmount);
            Debug.Log($"[DamageDealer] Dealt {damageAmount} damage. " +
                      $"Remaining HP: {targetHealth.CurrentHealth}");
        }

        if (Input.GetKeyDown(budDamageKey) && targetBud != null)
        {
            targetBud.TakeDamage(damageAmount);
            Debug.Log($"[DamageDealer] Bud HP: {targetBud.CurrentHealth}");
        }
    }
}
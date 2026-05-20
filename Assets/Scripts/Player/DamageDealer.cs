using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private float damageAmount = 50f;
    [SerializeField] private KeyCode damageKey = KeyCode.F;

    private EnemyHealth targetHealth;

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null) return;

        targetHealth = health;
        Debug.Log($"[DamageDealer] Enemy in range: {health.CurrentHealth} HP.");
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null) return;

        if (health == targetHealth)
        {
            targetHealth = null;
            Debug.Log("[DamageDealer] Enemy left range.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(damageKey) && targetHealth != null)
        {
            targetHealth.TakeDamage(damageAmount);
            Debug.Log($"[DamageDealer] Dealt {damageAmount} damage. " +
                      $"Remaining HP: {targetHealth.CurrentHealth}");
        }
    }
}
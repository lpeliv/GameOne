using UnityEngine;

public class TurretProjectile : MonoBehaviour
{
    private Vector3 targetPoint;
    private float speed;
    private float damage;
    private bool hasHit;

    [Header("Settings")]
    [SerializeField] private float maxLifetime = 5f;
    private float lifetimeTimer;

    public void Initialize(Vector3 targetPoint, float speed, float damage)
    {
        this.targetPoint = targetPoint;
        this.speed = speed;
        this.damage = damage;
        hasHit = false;
        lifetimeTimer = 0f;

        Vector3 direction = (targetPoint - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (hasHit) return;

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Debug.Log("[TurretProjectile] Lifetime expired, destroying.");
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (targetPoint - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        float distToTarget = Vector3.Distance(transform.position, targetPoint);
        if (distToTarget < 0.5f)
        {
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TurretProjectile] OnTriggerEnter fired: {other.gameObject.name}, hasHit: {hasHit}");

        if (hasHit) return;

        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health == null)
        {
            return;
        }

        hasHit = true;
        health.TakeDamage(damage);
        
        Destroy(gameObject);
    }
}
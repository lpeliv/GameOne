using UnityEngine;

public class TurretAddon : MonoBehaviour
{
    private AddonDefinition definition;
    private TurretCylinder attachedCylinder;
    private float fireTimer;
    private int currentTier;
    private float currentDamage;
    private float currentRange;
    private float currentFireRate;
    private float rotationSpeed;
    private bool isAttached;

    public float Range => currentRange;
    public float Damage => currentDamage;
    public float FireRate => currentFireRate;
    public int CurrentTier => currentTier;
    public bool IsAttached => isAttached;

    public void Initialize(AddonDefinition def)
    {
        definition = def;
        currentTier = 0;
        currentDamage = def.damage;
        currentRange = def.range;
        currentFireRate = def.fireRate;
        fireTimer = 0f;
        isAttached = false;
    }

    private void Update()
    {
        if (!isAttached) return;

        fireTimer += Time.deltaTime;
    }

    public void OnAttached(TurretCylinder cylinder)
    {
        attachedCylinder = cylinder;
        isAttached = true;
        fireTimer = 0f;
    }

    public void OnDetached()
    {
        attachedCylinder = null;
        isAttached = false;
        fireTimer = 0f;
    }

    public void Fire(EnemyPathFollower target)
    {
        if (!isAttached) return;
        if (definition == null) return;
        if (target == null) return;
        if (target.IsDead) return;

        if (fireTimer < 1f / currentFireRate) return;

        fireTimer = 0f;
        SpawnProjectile(target);
    }

    private void SpawnProjectile(EnemyPathFollower target)
    {
        if (definition.projectilePrefab == null)
        {
            Debug.LogWarning("[TurretAddon] No projectile prefab assigned.");
            return;
        }

        Vector3 interceptPoint = CalculateIntercept(target);

        GameObject go = Instantiate(definition.projectilePrefab, transform.position, Quaternion.identity);
        TurretProjectile projectile = go.GetComponent<TurretProjectile>();

        if (projectile == null)
        {
            Debug.LogWarning("[TurretAddon] Projectile prefab missing TurretProjectile component.");
            Destroy(go);
            return;
        }

        projectile.Initialize(interceptPoint, definition.projectileSpeed, currentDamage);
    }

    private Vector3 CalculateIntercept(EnemyPathFollower target)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 targetVelocity = target.Velocity;
        
        Vector3 intercept = targetPos;

        for (int i = 0; i < 3; i++)
        {
            float dist = Vector3.Distance(transform.position, intercept);
            float travelTime = dist / definition.projectileSpeed;
            intercept = targetPos + targetVelocity * travelTime;
        }

        return intercept;
    }

    public bool TryUpgrade()
    {
        if (definition.upgradeTiers == null || definition.upgradeTiers.Length == 0)
        {
            Debug.LogWarning("[TurretAddon] No upgrade tiers defined.");
            return false;
        }

        if (currentTier >= definition.upgradeTiers.Length)
        {
            Debug.LogWarning("[TurretAddon] Already at max tier.");
            return false;
        }

        AddonUpgradeTier tier = definition.upgradeTiers[currentTier];
        currentDamage *= tier.damageMultiplier;
        currentRange *= tier.rangeMultiplier;
        currentFireRate *= tier.fireRateMultiplier;
        rotationSpeed *= tier.rotationSpeedMultiplier;
        currentTier++;

        Debug.Log($"[TurretAddon] Upgraded to tier {currentTier}. Damage: {currentDamage}, Range: {currentRange}");
        return true;
    }
}
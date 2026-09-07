using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Offset")]
    [SerializeField] private float maxOffset = 0.3f;

    [Header("Grace Period")]
    [SerializeField] private float graceDuration = 2f;
    [SerializeField] private int waypointGraceCount = 2;

    private EnemyPath path;
    private Vector3 fixedOffset;
    private int currentWaypointIndex;
    private float segmentProgress;

    private EnemyState currentState = EnemyState.Idle;
    public EnemyState CurrentState => currentState;

    public bool IsMoving => currentState == EnemyState.Moving;
    public bool HasArrived => currentState == EnemyState.SeekingBud ||
                              currentState == EnemyState.AttackingBud;
    public bool IsDead => currentState == EnemyState.Dead;

    private float rolledSize;
    private float derivedSpeed;

    private float weight;
    public float Weight => weight;

    private float damage;
    private float attackRate;
    private float attackRange;
    private float attackTimer;

    public float AttackRange => attackRange;
    public int WaypointIndex => currentWaypointIndex;
    private Vector3 velocity;
    public Vector3 Velocity => velocity;

    private HealthBudManager healthBudManager;
    private HealthBud currentTargetBud;

    private Rigidbody rb;

    private Vector3 claimedAttackPosition;
    private bool hasClaimedPosition;

    private EnemyDefinition enemyDefinition;
    public EnemyDefinition Definition => enemyDefinition;

    private float detectionRange;
    private float meleeRange;
    private float behaviourQueryTimer;
    private float behaviourQueryInterval = 0.2f;

    private int cachedReturnIndex = 0;
    private float returnIndexTimer = 0f;
    private const float returnIndexInterval = 0.1f;

    private int hittableLayer;

    private float graceTimer = 0f;

    private float separationTimer = 0f;
    private const float separationInterval = 0.2f;
    private Vector3 cachedSeparationForce = Vector3.zero;

    private float returnBlend = 0f;
    private const float blendSpeed = 2f;
    private bool isBlending = false;
    private Vector3 blendStartPos;
    private Quaternion blendStartRot;
    private int blendTargetWaypointIndex = 0;

    private bool IsInGrace()
    {
        return graceTimer < graceDuration ||
               currentWaypointIndex < waypointGraceCount;
    }

    public void Die()
    {
        if (hasClaimedPosition && currentTargetBud != null)
            healthBudManager?.ReleaseAttackPosition(currentTargetBud, claimedAttackPosition);

        EnemyRegistry.Instance?.Unregister(this);
        currentState = EnemyState.Dead;
        Destroy(gameObject);
    }

    public void SetPath(EnemyPath enemyPath)
    {
        path = enemyPath;
        fixedOffset = new Vector3(
            Random.Range(-maxOffset, maxOffset),
            0f,
            Random.Range(-maxOffset, maxOffset)
        );
        currentWaypointIndex = 0;
        segmentProgress = 0f;
        graceTimer = 0f;
        currentState = EnemyState.Idle;
    }

    public void StartMoving()
    {
        if (path == null)
        {
            Debug.LogWarning("[EnemyPathFollower] No path assigned.");
            return;
        }
        currentState = EnemyState.Moving;
    }

    public void StopMoving()
    {
        currentState = EnemyState.Idle;
    }

    private void FixedUpdate()
    {
        if (currentState == EnemyState.Dead) return;

        if (graceTimer < graceDuration)
            graceTimer += Time.fixedDeltaTime;

        behaviourQueryTimer += Time.deltaTime;
        if (behaviourQueryTimer >= behaviourQueryInterval)
        {
            behaviourQueryTimer = 0f;
            CheckBehaviour();
        }

        switch (currentState)
        {
            case EnemyState.Moving:
                FollowPath();
                break;
            case EnemyState.SeekingBud:
                SeekBud();
                break;
            case EnemyState.AttackingBud:
                AttackBud();
                break;
            case EnemyState.SeekingPlayer:
                SeekPlayer();
                break;
            case EnemyState.AttackingPlayer:
                AttackPlayer();
                break;
            case EnemyState.ReturningToPath:
                ReturnToPath();
                break;
        }

        separationTimer += Time.fixedDeltaTime;
        if (separationTimer >= separationInterval)
        {
            separationTimer = 0f;
            cachedSeparationForce = CalculateSeparation();
        }

        if (cachedSeparationForce.sqrMagnitude > 0.001f)
            rb.MovePosition(rb.position + cachedSeparationForce * Time.fixedDeltaTime);
    }

    private void SeekBud()
    {
        if (healthBudManager == null) return;

        if (currentTargetBud == null || currentTargetBud.IsDestroyed)
        {
            if (hasClaimedPosition && currentTargetBud != null)
            {
                healthBudManager.ReleaseAttackPosition(currentTargetBud, claimedAttackPosition);
                hasClaimedPosition = false;
            }

            currentTargetBud = healthBudManager.GetClosestBud(transform.position);
            if (currentTargetBud == null) return;
        }

        if (!hasClaimedPosition)
        {
            claimedAttackPosition = healthBudManager.ClaimAttackPosition(currentTargetBud, attackRange);
            hasClaimedPosition = true;
        }

        Vector3 direction = claimedAttackPosition - transform.position;
        float distance = direction.magnitude;

        if (direction.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(
                transform.eulerAngles.y,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );
            transform.eulerAngles = new Vector3(0f, angle, 0f);
        }

        if (distance <= 1f)
        {
            currentState = EnemyState.AttackingBud;
            attackTimer = 0f;
            return;
        }

        rb.MovePosition(transform.position + direction.normalized * derivedSpeed * Time.deltaTime);
    }

    private void AttackBud()
    {
        rb.angularVelocity = Vector3.zero;
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);

        if (currentTargetBud == null || currentTargetBud.IsDestroyed)
        {
            if (hasClaimedPosition && currentTargetBud != null)
            {
                healthBudManager.ReleaseAttackPosition(currentTargetBud, claimedAttackPosition);
                hasClaimedPosition = false;
            }
            currentTargetBud = healthBudManager.GetClosestBud(transform.position);
            if (currentTargetBud == null) return;
            currentState = EnemyState.SeekingBud;
            return;
        }

        float distToClaimed = Vector3.Distance(transform.position, claimedAttackPosition);

        if (distToClaimed > attackRange * 2f)
        {
            currentState = EnemyState.SeekingBud;
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= 1f / attackRate)
        {
            attackTimer = 0f;
            currentTargetBud.TakeDamage(damage);
        }
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= path.Count - 1)
        {
            OnArrived();
            return;
        }

        Vector3 p1 = path.GetWaypoint(currentWaypointIndex);
        Vector3 p2 = path.GetWaypoint(Mathf.Min(currentWaypointIndex + 1, path.Count - 1));

        float segmentLength = Vector3.Distance(p1, p2);
        float step = segmentLength > 0.001f ? derivedSpeed * Time.deltaTime / segmentLength : 0f;

        segmentProgress += step;

        Vector3 position = EvaluateCatmullRom(
            currentWaypointIndex,
            segmentProgress
        ) + fixedOffset;

        velocity = (position - transform.position) / Time.deltaTime;

        Vector3 tangent = EvaluateCatmullRomTangent(currentWaypointIndex, segmentProgress);
        if (tangent.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(tangent.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        rb.MovePosition(position);

        if (segmentProgress >= 1f)
        {
            segmentProgress -= 1f;
            currentWaypointIndex++;
        }
    }

    private Vector3 EvaluateCatmullRom(int index, float t)
    {
        Vector3 p0 = path.GetWaypoint(Mathf.Max(index - 1, 0));
        Vector3 p1 = path.GetWaypoint(index);
        Vector3 p2 = path.GetWaypoint(Mathf.Min(index + 1, path.Count - 1));
        Vector3 p3 = path.GetWaypoint(Mathf.Min(index + 2, path.Count - 1));

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 EvaluateCatmullRomTangent(int index, float t)
    {
        Vector3 p0 = path.GetWaypoint(Mathf.Max(index - 1, 0));
        Vector3 p1 = path.GetWaypoint(index);
        Vector3 p2 = path.GetWaypoint(Mathf.Min(index + 1, path.Count - 1));
        Vector3 p3 = path.GetWaypoint(Mathf.Min(index + 2, path.Count - 1));

        float t2 = t * t;

        return 0.5f * (
            (-p0 + p2) +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * 2f * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * 3f * t2
        );
    }

    private void OnArrived()
    {
        transform.position = path.GetWaypoint(path.Count - 1);

        switch (enemyDefinition.behaviour)
        {
            case EnemyBehaviour.TargetBuds:
                currentState = EnemyState.SeekingBud;
                break;
            case EnemyBehaviour.TargetPlayer:
                currentState = EnemyState.SeekingPlayer;
                break;
            default:
                currentState = EnemyState.SeekingBud;
                break;
        }

        Debug.Log($"[EnemyPathFollower] Arrived. Behaviour: {enemyDefinition?.behaviour}");
    }

    public void Initialize(EnemyDefinition definition, float statMultiplier = 1f)
    {
        enemyDefinition = definition;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.mass = rolledSize * statMultiplier;
        rolledSize = definition.RollSize() * statMultiplier;
        derivedSpeed = definition.DeriveSpeed(rolledSize) * statMultiplier;
        moveSpeed = derivedSpeed;
        weight = definition.weight * statMultiplier;

        damage = definition.baseDamage * statMultiplier * rolledSize;
        attackRate = definition.baseAttackRate / (rolledSize * statMultiplier);
        attackRange = definition.baseAttackRange;
        detectionRange = definition.detectionRange;
        meleeRange = definition.meleeRange;

        transform.localScale = Vector3.one * rolledSize;

        hittableLayer = LayerMask.GetMask("Hittable");

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.Initialize(definition, rolledSize, statMultiplier);
        else
            Debug.LogWarning("[EnemyPathFollower] No EnemyHealth component found on enemy prefab.");
    }

    public void SetHealthBudManager(HealthBudManager manager)
    {
        healthBudManager = manager;
    }

    private void CheckBehaviour()
    {
        if (IsInGrace()) return;

        if (PlayerInventory.Instance == null) return;
        if (enemyDefinition == null) return;

        float distToPlayer = Vector3.Distance(
            transform.position,
            PlayerInventory.Instance.transform.position
        );

        if (distToPlayer <= meleeRange &&
            currentState != EnemyState.Dead &&
            currentState != EnemyState.SeekingBud &&
            currentState != EnemyState.AttackingBud)
        {
            currentState = EnemyState.AttackingPlayer;
            return;
        }

        if (enemyDefinition.behaviour == EnemyBehaviour.TargetPlayer)
        {
            bool canChase = currentState == EnemyState.Moving ||
                            currentState == EnemyState.ReturningToPath;

            bool shouldReturn = currentState == EnemyState.SeekingPlayer &&
                                distToPlayer > detectionRange;

            if (distToPlayer <= detectionRange &&
                distToPlayer > meleeRange &&
                canChase)
            {
                currentState = EnemyState.SeekingPlayer;
                return;
            }

            if (currentState == EnemyState.AttackingPlayer &&
                distToPlayer > meleeRange)
            {
                currentState = EnemyState.SeekingPlayer;
                return;
            }

            if (shouldReturn)
            {
                currentState = EnemyState.ReturningToPath;
                returnIndexTimer = returnIndexInterval;
                isBlending = false;
                returnBlend = 0f;
                return;
            }
        }
        else
        {
            if (currentState == EnemyState.AttackingPlayer &&
                distToPlayer > meleeRange)
            {
                currentState = EnemyState.ReturningToPath;
                returnIndexTimer = returnIndexInterval;
                isBlending = false;
                returnBlend = 0f;
                return;
            }
        }
    }

    private void SeekPlayer()
    {
        Vector3 playerPos = PlayerInventory.Instance.transform.position;
        Vector3 direction = playerPos - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(
                transform.eulerAngles.y,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );
            transform.eulerAngles = new Vector3(0f, angle, 0f);
        }

        Vector3 dir = direction.normalized;
        bool wallBlocking = Physics.SphereCast(
            transform.position,
            0.3f,
            dir,
            out RaycastHit wallHit,
            derivedSpeed * Time.fixedDeltaTime * 2f,
            LayerMask.GetMask("Default", "Wall")
        );

        if (wallBlocking)
        {
            currentState = EnemyState.ReturningToPath;
            returnIndexTimer = returnIndexInterval;
            isBlending = false;
            returnBlend = 0f;
            return;
        }

        rb.MovePosition(transform.position + dir * derivedSpeed * Time.deltaTime);
    }

    private void ReturnToPath()
    {
        if (path == null) return;

        if (isBlending)
        {
            returnBlend += Time.fixedDeltaTime * blendSpeed;
            returnBlend = Mathf.Clamp01(returnBlend);

            Vector3 targetPos = EvaluateCatmullRom(blendTargetWaypointIndex, 0f) + fixedOffset;

            Vector3 tangent = EvaluateCatmullRomTangent(blendTargetWaypointIndex, 0f);
            Quaternion targetRot = tangent.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(tangent.normalized)
                : transform.rotation;

            Vector3 blendPos = Vector3.Lerp(blendStartPos, targetPos, returnBlend);
            Quaternion blendRot = Quaternion.Slerp(blendStartRot, targetRot, returnBlend);

            rb.MovePosition(blendPos);
            transform.rotation = blendRot;

            if (returnBlend >= 1f)
            {
                isBlending = false;
                returnBlend = 0f;
                currentState = EnemyState.Moving;
                segmentProgress = 0f;
            }

            return;
        }

        returnIndexTimer += Time.fixedDeltaTime;

        float distToTarget = Vector3.Distance(
            transform.position,
            path.GetWaypoint(cachedReturnIndex) + fixedOffset);

        bool isClose = distToTarget < MasterManager.TileScale * 2f;

        if (isClose || returnIndexTimer >= returnIndexInterval)
        {
            returnIndexTimer = 0f;
            cachedReturnIndex = FindNearestWaypointIndex();
        }

        Vector3 target = path.GetWaypoint(cachedReturnIndex) + fixedOffset;
        Vector3 direction = (target - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(
                transform.eulerAngles.y,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );
            transform.eulerAngles = new Vector3(0f, angle, 0f);
        }

        float returnSpeed = derivedSpeed * 0.5f;
        rb.MovePosition(transform.position + direction.normalized * returnSpeed * Time.fixedDeltaTime);

        float snapThreshold = Mathf.Max(derivedSpeed * Time.fixedDeltaTime * 1.5f, 0.5f);
        if (direction.magnitude < snapThreshold)
        {
            isBlending = true;
            returnBlend = 0f;
            blendStartPos = transform.position;
            blendStartRot = transform.rotation;
            blendTargetWaypointIndex = cachedReturnIndex;
            currentWaypointIndex = cachedReturnIndex;
            segmentProgress = 0f;
        }
    }

    private int FindNearestWaypointIndex()
    {
        int targetIndex = 0;
        float closestDist = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 waypointPos = path.GetWaypoint(i) + fixedOffset;
            float dist = Vector3.Distance(transform.position, waypointPos);

            if (dist < closestDist)
            {
                closestDist = dist;
                targetIndex = i;
            }
        }

        return targetIndex;
    }

    private Vector3 CalculateSeparation()
    {
        Collider[] nearby = Physics.OverlapSphere(
            transform.position,
            rolledSize * 1.5f,
            LayerMask.GetMask("Enemy")
        );

        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (Collider col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 away = transform.position - col.transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist < 0.001f)
            {
                away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                dist = 0.1f;
            }
            float overlap = rolledSize * 1.5f - dist;
            if (overlap > 0f)
            {
                separation += away.normalized * overlap;
                count++;
            }
        }

        if (count > 0) separation /= count;
        return separation * 2f;
    }

    private void AttackPlayer()
    {
        if (PlayerHealth.Instance == null) return;

        float distToPlayer = Vector3.Distance(
            transform.position,
            PlayerInventory.Instance.transform.position
        );

        if (distToPlayer > meleeRange)
        {
            currentState = EnemyState.SeekingPlayer;
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= 1f / attackRate)
        {
            attackTimer = 0f;
            PlayerHealth.Instance.TakeDamage(damage);
            Debug.Log($"[EnemyPathFollower] Attacked player for {damage}.");
        }
    }
}

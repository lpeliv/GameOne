using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Offset")]
    [SerializeField] private float maxOffset = 0.3f;

    private EnemyPath path;
    private Vector3 fixedOffset;
    private int currentWaypointIndex;
    private float segmentProgress;
    private bool isMoving;
    private bool hasArrived;

    public bool IsMoving => isMoving;
    public bool HasArrived => hasArrived;

    private float rolledSize;
    private float derivedSpeed;

    private float weight;
    public float Weight => weight;

    private bool isDead;
    public bool IsDead => isDead;

    public void Die()
    {
        isDead = true;
        isMoving = false;
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
        isMoving = false;
        hasArrived = false;
    }

    public void StartMoving()
    {
        if (path == null)
        {
            Debug.LogWarning("[EnemyPathFollower] No path assigned. Call SetPath first.");
            return;
        }
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    private void Update()
    {
        if (!isMoving || hasArrived) return;

        FollowPath();
    }

    private void FollowPath()
    {
        if (currentWaypointIndex >= path.Count - 1)
        {
            OnArrived();
            return;
        }

        segmentProgress += moveSpeed * Time.deltaTime;

        Vector3 position = EvaluateCatmullRom(
            currentWaypointIndex,
            segmentProgress
        ) + fixedOffset;

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

        transform.position = position;

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
        isMoving = false;
        hasArrived = true;
        transform.position = path.GetWaypoint(path.Count - 1) + fixedOffset;
    }

    public void Initialize(EnemyDefinition definition, float statMultiplier = 1f)
    {
        rolledSize = definition.RollSize() * statMultiplier;
        derivedSpeed = definition.DeriveSpeed(rolledSize) * statMultiplier;
        moveSpeed = derivedSpeed;
        weight = definition.weight * statMultiplier;

        transform.localScale = Vector3.one * rolledSize;

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.Initialize(definition, rolledSize, statMultiplier);
        else
            Debug.LogWarning("[EnemyPathFollower] No EnemyHealth component found on enemy prefab.");
    }
}
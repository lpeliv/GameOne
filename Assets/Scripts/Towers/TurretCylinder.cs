using UnityEngine;

public class TurretCylinder : MonoBehaviour
{
    [Header("Joint")]
    [SerializeField] private TurretJoint joint;

    private float rotationSpeed;
    private float queryInterval = 0.2f;
    private float queryTimer;
    private bool isActive;
    private EnemyPathFollower currentTarget;
    private TargetingPriority targetingPriority;

    [Header("Rotation")]
    [SerializeField] private float rotationOffset = 0f;

    public TurretAddon CurrentAddon => joint?.CurrentAddon;
    public bool HasAddon => joint != null && joint.IsOccupied;
    public TurretJoint Joint => joint;

    public void Initialize(float rotSpeed)
    {
        rotationSpeed = rotSpeed;
        queryTimer = 0f;
        isActive = true;
        currentTarget = null;
        targetingPriority = TargetingPriority.Closest;
    }

    public void Deactivate()
    {
        isActive = false;
        currentTarget = null;
    }

    private void Update()
    {
        if (!isActive || !HasAddon) return;

        queryTimer += Time.deltaTime;
        if (queryTimer >= queryInterval)
        {
            queryTimer = 0f;
            UpdateTarget();
        }

        if (currentTarget != null)
            RotateTowardTarget();
    }

    public void SetTargetingPriority(TargetingPriority priority)
    {
        targetingPriority = priority;
    }

    private void UpdateTarget()
    {
        if (EnemyRegistry.Instance == null) return;
        if (joint?.CurrentAddon == null) return;

        float range = joint.CurrentAddon.Range;

        currentTarget = targetingPriority switch
        {
            TargetingPriority.Closest => EnemyRegistry.Instance.GetClosestEnemy(transform.position, range),
            TargetingPriority.FirstInLine => EnemyRegistry.Instance.GetFirstInLine(transform.position, range),
            TargetingPriority.HighestHP => EnemyRegistry.Instance.GetHighestHP(transform.position, range),
            TargetingPriority.LowestHP => EnemyRegistry.Instance.GetLowestHP(transform.position, range),
            _ => null
        };
    }

    private void RotateTowardTarget()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            return;
        }

        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0f;



        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction) *
                                    Quaternion.Euler(0f, rotationOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        Vector3 correctedForward = Quaternion.Euler(0f, rotationOffset, 0f) * transform.forward;
        float angle = Vector3.Angle(correctedForward, direction.normalized);

        if (joint?.CurrentAddon != null && IsAlignedWithTarget())
            joint.CurrentAddon.Fire(currentTarget);
    }

    private bool IsAlignedWithTarget()
    {
        if (currentTarget == null) return false;

        Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
        directionToTarget.y = 0f;

        Vector3 correctedForward = Quaternion.Euler(0f, -rotationOffset, 0f) * transform.forward;
        correctedForward.y = 0f;

        float angle = Vector3.Angle(correctedForward, directionToTarget);
        return angle < 5f;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBase : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private List<Transform> outpostWaypoints;
    [SerializeField] private List<Transform> houseWaypoints;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float waypointThreshold = 0.5f;

    private bool isMoving = false;
    private int currentWaypointIndex = 0;
    private List<Transform> currentPath;

    public bool IsMoving => isMoving;

    public void WalkToHouse()
    {
        if (isMoving) return;
        StartCoroutine(FollowPath(houseWaypoints));
    }

    public void WalkToOutpost()
    {
        if (isMoving) return;
        StartCoroutine(FollowPath(outpostWaypoints));
    }

    private IEnumerator FollowPath(List<Transform> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0) yield break;

        isMoving = true;
        currentWaypointIndex = 0;
        currentPath = waypoints;

        while (currentWaypointIndex < waypoints.Count)
        {
            Transform target = waypoints[currentWaypointIndex];
            Vector3 targetPos = new Vector3(target.position.x, transform.position.y, target.position.z);

            while (Vector3.Distance(transform.position, targetPos) > waypointThreshold)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                Vector3 direction = (targetPos - transform.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }

                yield return null;
            }

            currentWaypointIndex++;
        }

        isMoving = false;
        OnPathComplete();
    }

    protected virtual void OnPathComplete()
    {
        // Override in subclass for specific behaviour
        // e.g. play idle animation, face a direction
    }
}
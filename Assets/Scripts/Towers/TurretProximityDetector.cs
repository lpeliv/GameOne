using System.Collections.Generic;
using UnityEngine;

public class TurretProximityDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float proximityRadius = 15f;
    [SerializeField] private LayerMask playerLayer;

    private List<TurretJoint> joints = new List<TurretJoint>();
    private bool playerNearby = false;

    private void Awake()
    {
        joints.AddRange(GetComponentsInChildren<TurretJoint>());

        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.radius = proximityRadius;
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerNearby = false;
        ClearAllHighlights();
    }

    public void ClearAllHighlights()
    {
        foreach (TurretJoint joint in joints)
            joint.SetHighlight(JointHighlightState.None);
    }

    public IReadOnlyList<TurretJoint> Joints => joints;
    public bool PlayerNearby => playerNearby;

    private bool IsPlayer(Collider other) =>
        ((1 << other.gameObject.layer) & playerLayer) != 0;
}
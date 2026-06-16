using UnityEngine;

public class AddonInteractionDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AddonCarrySystem carrySystem;

    [Header("Detection")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactLayer;

    private TurretJoint lastHighlightedJoint;
    private TurretAddon lastHighlightedAddon;

    private void Update()
    {
        DetectInteractable();
    }

    private void HandleJointDetected(TurretJoint joint)
    {

        if (lastHighlightedJoint != null && lastHighlightedJoint != joint)
            lastHighlightedJoint.SetHighlight(JointHighlightState.None);

        lastHighlightedJoint = joint;

        TurretProximityDetector proximity = joint.GetComponentInParent<TurretProximityDetector>();
        if (proximity != null && !proximity.PlayerNearby)
        {
            joint.SetHighlight(JointHighlightState.None);
            return;
        }

        TurretBase turretBase = joint.GetComponentInParent<TurretBase>();
        if (turretBase != null && !turretBase.IsBuilt)
        {
            Debug.LogWarning("[AddonInteractionDetector] Turret not fully built yet.");
            return;
        }

        if (carrySystem.IsCarrying)
        {
            joint.SetHighlight(joint.IsOccupied
                ? JointHighlightState.Occupied
                : JointHighlightState.Empty);
        }
        else if (joint.IsOccupied)
        {
            joint.SetHighlight(JointHighlightState.Interactable);
        }
    }

    private void HandleAddonDetected(TurretAddon addon)
    {
        if (addon.IsAttached) return;

        if (lastHighlightedAddon != null && lastHighlightedAddon != addon)
            ClearAddonHighlight(lastHighlightedAddon);

        lastHighlightedAddon = addon;
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            TurretJoint joint = hit.collider.GetComponentInParent<TurretJoint>();
            if (joint != null)
            {
                HandleJointDetected(joint);
                return;
            }

            TurretAddon addon = hit.collider.GetComponentInParent<TurretAddon>();
            if (addon != null)
            {
                HandleAddonDetected(addon);
                return;
            }
        }

        ClearHighlights();
    }

    public void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer)) return;

        TurretJoint joint = hit.collider.GetComponentInParent<TurretJoint>();
        if (joint != null)
        {
            HandleJointInteract(joint);
            return;
        }

        TurretAddon addon = hit.collider.GetComponentInParent<TurretAddon>();
        if (addon != null)
        {
            HandleAddonInteract(addon);
            return;
        }
    }

    private void HandleJointInteract(TurretJoint joint)
    {
        TurretBase turretBase = joint.GetComponentInParent<TurretBase>();
        if (turretBase != null && !turretBase.IsBuilt)
        {
            Debug.LogWarning("[AddonInteractionDetector] Turret not fully built yet.");
            return;
        }

        if (carrySystem.IsCarrying)
        {
            TurretAddon swapped = carrySystem.PlaceOnJoint(joint);
            if (swapped != null)
                carrySystem.PickUp(swapped);

            ClearHighlights();
        }
        else if (joint.IsOccupied)
        {
            TurretAddon addon = joint.Detach();
            carrySystem.PickUp(addon);
            joint.SetHighlight(JointHighlightState.None);
        }
    }

    private void HandleAddonInteract(TurretAddon addon)
    {
        if (carrySystem.IsCarrying) return;
        if (addon.IsAttached) return;

        carrySystem.PickUp(addon);
    }

    private void ClearHighlights()
    {
        if (lastHighlightedJoint != null)
        {
            lastHighlightedJoint.SetHighlight(JointHighlightState.None);
            lastHighlightedJoint = null;
        }

        if (lastHighlightedAddon != null)
        {
            ClearAddonHighlight(lastHighlightedAddon);
            lastHighlightedAddon = null;
        }
    }

    private void ClearAddonHighlight(TurretAddon addon)
    {
        lastHighlightedAddon = null;
    }
}
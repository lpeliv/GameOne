using TMPro;
using UnityEngine;

public class AddonInteractionDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AddonCarrySystem carrySystem;

    [Header("Detection")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    private TurretJoint lastHighlightedJoint;
    private TurretAddon lastHighlightedAddon;
    private ShopUIBase[] shops;

    private void Awake()
    {
        shops = FindObjectsByType<ShopUIBase>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (IsAnyShopOpen()) return;
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
            if (((1 << hit.collider.gameObject.layer) & interactLayer) != 0)
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

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    ShowPrompt(interactable.InteractionPrompt);
                    return;
                }
            }
        }

        ClearHighlights();
        HidePrompt();
    }

    public void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer)) return;
        if (((1 << hit.collider.gameObject.layer) & interactLayer) == 0) return;

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

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactable.OnInteract();
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
            Debug.Log($"[AddonInteractionDetector] Placing carried addon on joint. Joint occupied: {joint.IsOccupied}");
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

    private void ShowPrompt(string message)
    {
        if (interactionPromptText == null) return;
        interactionPromptText.gameObject.SetActive(true);
        interactionPromptText.text = message;
    }

    public void HidePrompt()
    {
        if (interactionPromptText == null) return;
        interactionPromptText.gameObject.SetActive(false);
    }

    private bool IsAnyShopOpen()
    {
        foreach (ShopUIBase shop in shops)
            if (shop != null && shop.IsOpen) return true;
        return false;
    }
}
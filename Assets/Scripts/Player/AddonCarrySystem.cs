using UnityEngine;

public class AddonCarrySystem : MonoBehaviour
{
    [Header("Hold Position")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float carrySmoothing = 10f;

    [SerializeField] private GameObject hammerRoot;

    private TurretAddon carriedAddon;

    public bool IsCarrying => carriedAddon != null;
    public TurretAddon CarriedAddon => carriedAddon;

    public void PickUp(TurretAddon addon)
    {
        if (addon == null) return;
        if (IsCarrying) return;

        carriedAddon = addon;
        carriedAddon.transform.SetParent(holdPoint);
        carriedAddon.transform.localPosition = Vector3.zero;
        carriedAddon.transform.localRotation = Quaternion.identity;

        Debug.Log($"[AddonCarrySystem] Picked up {addon.name}.");

        hammerRoot?.SetActive(false);
    }

    public TurretAddon Drop()
    {
        if (!IsCarrying) return null;

        TurretAddon addon = carriedAddon;
        carriedAddon = null;
        addon.transform.SetParent(null);

        Debug.Log($"[AddonCarrySystem] Dropped {addon.name}.");
        hammerRoot?.SetActive(true);
        return addon;
    }

    public TurretAddon PlaceOnJoint(TurretJoint joint)
    {
        if (!IsCarrying) return null;
        if (joint == null) return null;

        TurretAddon addonToPlace = carriedAddon;
        TurretAddon swappedAddon = null;

        if (joint.IsOccupied)
        {
            swappedAddon = joint.Detach();
            swappedAddon.transform.SetParent(null);
        }

        carriedAddon = null;
        addonToPlace.transform.SetParent(null);

        joint.Attach(addonToPlace);

        Debug.Log($"[AddonCarrySystem] Placed {addonToPlace.name} on joint.");
        hammerRoot?.SetActive(true);
        return swappedAddon;
    }

    private void Update()
    {
        if (!IsCarrying) return;
        SmoothHoldPosition();
    }

    private void SmoothHoldPosition()
    {
        carriedAddon.transform.localPosition = Vector3.Lerp(
            carriedAddon.transform.localPosition,
            Vector3.zero,
            carrySmoothing * Time.deltaTime
        );

        carriedAddon.transform.localRotation = Quaternion.Slerp(
            carriedAddon.transform.localRotation,
            Quaternion.identity,
            carrySmoothing * Time.deltaTime
        );
    }
}
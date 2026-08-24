using System.Collections;
using UnityEngine;

public class ZoneDoor : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private Side zone;

    [Header("Door")]
    [SerializeField] private Transform doorMesh;
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float animSpeed = 2f;

    private ZoneDoorState state = ZoneDoorState.Locked;
    private Coroutine animCoroutine;

    public Side Zone => zone;
    public ZoneDoorState State => state;

    private void Start()
    {
        SetState(zone == Side.Left ? ZoneDoorState.Open : ZoneDoorState.Locked);
    }

    public void SetState(ZoneDoorState newState)
    {
        state = newState;

        bool shouldOpen = newState == ZoneDoorState.Open;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(
            AnimateDoor(shouldOpen ? openAngle : closedAngle));

        Debug.Log($"[ZoneDoor] Zone {zone} door: {state}");
    }

    private IEnumerator AnimateDoor(float targetAngle)
    {
        Quaternion startRot = doorMesh.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;
            doorMesh.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        doorMesh.localRotation = targetRot;
        animCoroutine = null;
    }
}
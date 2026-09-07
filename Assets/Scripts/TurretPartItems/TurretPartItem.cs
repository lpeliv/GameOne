using UnityEngine;

public class TurretPartItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string blueprintId;
    [SerializeField] private int partIndex;
    [SerializeField] private string partName;

    [Header("Blueprint Target")]
    [SerializeField] private TurretDefinition baseBlueprintTarget;
    [SerializeField] private AddonDefinition addonBlueprintTarget;

    public bool IsBaseBlueprint => baseBlueprintTarget != null;

    public string InteractionPrompt => $"Pick up {partName} [E]";

    public void OnInteract()
    {
        Debug.Log($"[TurretPartItem] PlayerInventory.Instance: {PlayerInventory.Instance != null}");
        Debug.Log($"[TurretPartItem] Attempting AddPart for id: '{blueprintId}'");
        var progress = PlayerInventory.Instance.BlueprintProgress;
        bool added = progress.AddPart(blueprintId);
        Debug.Log($"[TurretPartItem] Parts now: {progress.GetPartsFound(blueprintId)}");

        if (added)
            Debug.Log($"[TurretPartItem] Picked up {partName} (part {partIndex}) for blueprint '{blueprintId}'. Parts: {progress.GetPartsFound(blueprintId)}/3");
        else
            Debug.Log($"[TurretPartItem] Blueprint '{blueprintId}' already complete.");

        gameObject.SetActive(false);

        if (progress.IsUnlocked(blueprintId))
            Debug.Log($"[TurretPartItem] Blueprint {blueprintId} fully unlocked!");
    }
}

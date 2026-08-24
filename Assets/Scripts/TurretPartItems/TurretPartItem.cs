using UnityEngine;

public class TurretPartItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string blueprintId;
    [SerializeField] private int partIndex;
    [SerializeField] private string partName;

    public string InteractionPrompt => $"Pick up {partName} [E]";

    public void OnInteract()
    {
        var progress = PlayerInventory.Instance.BlueprintProgress;
        bool added = progress.AddPart(blueprintId);

        if (added)
            Debug.Log($"[TurretPartItem] Picked up {partName} (part {partIndex}) for blueprint '{blueprintId}'. Parts: {progress.GetPartsFound(blueprintId)}/3");
        else
            Debug.Log($"[TurretPartItem] Blueprint '{blueprintId}' already complete.");

        gameObject.SetActive(false);

        if (progress.IsUnlocked(blueprintId))
            Debug.Log($"[TurretPartItem] Blueprint {blueprintId} fully unlocked!");
    }
}

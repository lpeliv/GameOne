using UnityEngine;

public class WitchNPC : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 5f;

    [SerializeField] private WitchShopUI shopUI;

    public string InteractionPrompt => "Talk to Witch [E]";

    public void OnInteract()
    {
        shopUI?.Show();
    }
}
using UnityEngine;

public class WitchNPC : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 5f;

    public string InteractionPrompt => "Talk to Witch [E]";

    public void OnInteract()
    {
        WitchShopUI.Instance?.Show();
    }
}
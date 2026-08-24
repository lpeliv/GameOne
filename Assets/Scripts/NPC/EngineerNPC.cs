using UnityEngine;

public class EngineerNPC : NPCBase, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 5f;

    [SerializeField] private EngineerShopUI shopUI;

    public string InteractionPrompt => "Talk to Engineer [E]";

    public void OnInteract()
    {
        shopUI?.Show();
    }
}

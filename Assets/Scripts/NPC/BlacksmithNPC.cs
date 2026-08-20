using UnityEngine;

public class BlacksmithNPC : NPCBase, IInteractable
{
    [SerializeField] private BlacksmithShopUI shopUI;

    public string InteractionPrompt => "Talk to Blacksmith [E]";

    public void OnInteract()
    {
        shopUI?.Show();
    }
}
public interface IInteractable
{
    void OnInteract();
    string InteractionPrompt { get; }
}
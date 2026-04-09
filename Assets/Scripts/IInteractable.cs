public interface IInteractable
{
    string GetInteractionPrompt();
    void Interact(PlayerMovement player);
}
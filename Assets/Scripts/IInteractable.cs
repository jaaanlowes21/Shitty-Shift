using UnityEngine;

public interface IInteractable
{
    float interactionRadius { get; }
    string GetInteractionPrompt();
    void Interact(PlayerMovement player);
}
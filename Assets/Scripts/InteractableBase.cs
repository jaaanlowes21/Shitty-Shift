using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    public string interactionPrompt = "Press F to Interact";
    public float interactionRadius = 2.5f;

    // Explicit interface implementation
    float IInteractable.interactionRadius => interactionRadius;

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public abstract void Interact(PlayerMovement player);
}
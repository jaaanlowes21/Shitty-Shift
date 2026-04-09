using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] protected string interactionPrompt = "Press F to Interact";

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public abstract void Interact(PlayerMovement player);
}
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] protected string interactionPrompt = "Press F to Interact";
    [Header("Interaction Radius")]
    public float interactionRadius = 2f;

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public abstract void Interact(PlayerMovement player);
}
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    public string interactionPrompt = "Press F to Interact";
    public float interactionRadius = 2.5f;

    [Header("Outline")]
    public Outline outlineComponent;
    public bool useOutline = true;
    public float outlineShowDistance = 10f;

    private Transform player;
    private bool outlineEnabled = false;

    float IInteractable.interactionRadius => interactionRadius;

    protected virtual void Start()
    {
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
                outlineComponent = GetComponentInChildren<Outline>();
        }

        if (outlineComponent != null)
            outlineComponent.enabled = false;

        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm.transform;
    }

    protected virtual void Update()
    {
        if (!useOutline || outlineComponent == null || player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= outlineShowDistance)
        {
            if (!outlineEnabled)
            {
                outlineEnabled = true;
                outlineComponent.enabled = true;
            }
        }
        else
        {
            if (outlineEnabled)
            {
                outlineEnabled = false;
                outlineComponent.enabled = false;
            }
        }
    }

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public abstract void Interact(PlayerMovement player);
}
using System.Collections;
using UnityEngine;

public class ClueObject : InteractableBase
{
    public enum ClueType
    {
        Chair,
        Computer,
        Custom
    }

    [Header("Clue Settings")]
    public ClueType clueType = ClueType.Chair;
    
    [Header("Custom Prompts")]
    public string approachPrompt = "Check Chair";
    [TextArea(2, 4)]
    public string interactHint = "There's blood in this chair, could this be a clue?";

    [Header("Outline")]
    public Outline outlineComponent;
    public bool useOutline = true;
    public float outlineShowDistance = 2f;

    private bool hasBeenInteracted = false;
    private Transform player;
    private bool outlineEnabled = false;

    private void Reset()
    {
        interactionPrompt = "Check";
        interactionRadius = 2f;
        outlineShowDistance = 2f;
        UpdatePromptsFromType();
    }

    private void OnValidate()
    {
        if (clueType != ClueType.Custom)
        {
            UpdatePromptsFromType();
        }
    }

    protected override void Start()
    {
        // Find outline if not set
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
                outlineComponent = GetComponentInChildren<Outline>();
        }

        // FORCE disable outline at start
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            outlineEnabled = false;
        }

        // Find player
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm.transform;

        if (clueType != ClueType.Custom)
        {
            UpdatePromptsFromType();
        }
    }

    protected override void Update()
    {
        HandleOutline();
    }

    private void HandleOutline()
    {
        if (!useOutline || outlineComponent == null)
            return;

        // Keep trying to find player if lost
        if (player == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
                player = pm.transform;
            else
                return;
        }

        // Don't show outline if already interacted
        if (hasBeenInteracted)
        {
            if (outlineEnabled)
            {
                outlineEnabled = false;
                outlineComponent.enabled = false;
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool shouldShow = distance <= outlineShowDistance;

        if (shouldShow != outlineEnabled)
        {
            outlineEnabled = shouldShow;
            outlineComponent.enabled = shouldShow;
            Debug.Log($"[ClueObject] {name}: Outline {(shouldShow ? "ON" : "OFF")} - Distance: {distance:F1}");
        }
    }

    private void UpdatePromptsFromType()
    {
        switch (clueType)
        {
            case ClueType.Chair:
                approachPrompt = "Check Chair";
                interactHint = "There's blood in this chair, could this be a clue?";
                break;

            case ClueType.Computer:
                approachPrompt = "Check Computer";
                interactHint = "It's showing error, could this be a clue?";
                break;

            case ClueType.Custom:
                break;
        }
        
        interactionPrompt = approachPrompt;
    }

    public override string GetInteractionPrompt()
    {
        if (hasBeenInteracted)
            return "";
        
        return approachPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (hasBeenInteracted)
            return;

        hasBeenInteracted = true;

        // Turn off outline after interaction
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            outlineEnabled = false;
        }

        // Show the hint
        if (HintManager.Instance != null)
        {
            HintManager.Instance.ShowHint(interactHint);
        }
        else
        {
            Debug.LogWarning("[ClueObject] HintManager Instance not found!");
        }
        
        Debug.Log($"[ClueObject] {clueType}: Player interacted - showing hint: {interactHint}");
    }

    public void ResetClue()
    {
        hasBeenInteracted = false;
    }

    public bool HasBeenInteracted => hasBeenInteracted;

    private void OnDrawGizmosSelected()
    {
        // Draw outline distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outlineShowDistance);
        
        // Draw interaction radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
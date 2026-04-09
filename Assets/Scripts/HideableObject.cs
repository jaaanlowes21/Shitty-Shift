using UnityEngine;

public class HideableObject : InteractableBase
{
    [Header("Hide Positions")]
    public Transform hidePoint;
    public Transform exitPoint;

    private void Reset()
    {
        interactionPrompt = "Press F to Hide";
    }

    public override string GetInteractionPrompt()
    {
        if (hidePoint == null || exitPoint == null)
            return "Hide Spot Incomplete";

        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (player == null) return;

        if (!player.IsHidden)
        {
            player.EnterHide(this);
        }
        else if (player.GetHiddenInside() == this)
        {
            player.ExitHide();
        }
    }

    private void OnDrawGizmos()
    {
        if (hidePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(hidePoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, hidePoint.position);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(exitPoint.position, 0.15f);
            Gizmos.DrawLine(transform.position, exitPoint.position);
        }
    }
}
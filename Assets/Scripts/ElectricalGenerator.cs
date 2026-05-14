using UnityEngine;

public class ElectricalGenerator : InteractableBase
{
    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;

    private void Reset()
    {
        interactionPrompt = "Press F to Inspect Generator";
        interactionRadius = 2.5f;
    }

    public override string GetInteractionPrompt()
    {
        if (puzzle == null)
            return interactionPrompt;

        if (puzzle.IsCarryingFuel)
            return "Press F to Fill Generator";

        if (puzzle.HasFuel)
            return "Generator Fueled";

        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (puzzle == null)
            return;

        puzzle.InspectGenerator();
    }
}

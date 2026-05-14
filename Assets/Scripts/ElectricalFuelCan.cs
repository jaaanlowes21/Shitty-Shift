using UnityEngine;

public class ElectricalFuelCan : InteractableBase
{
    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;
    public bool hideAfterPickup = true;

    private bool pickedUp;

    private void Reset()
    {
        interactionPrompt = "Press F to Pick Up Fuel";
        interactionRadius = 2.5f;
    }

    public override string GetInteractionPrompt()
    {
        return pickedUp ? "Fuel Picked Up" : interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (pickedUp || puzzle == null)
            return;

        if (!puzzle.PickupFuel())
            return;

        pickedUp = true;

        if (hideAfterPickup)
            gameObject.SetActive(false);
    }
}

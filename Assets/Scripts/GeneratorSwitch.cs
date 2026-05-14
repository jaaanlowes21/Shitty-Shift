using UnityEngine;

public class GeneratorSwitch : InteractableBase
{
    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;

    [Header("Visuals")]
    public Transform switchHandle;
    public Vector3 offEulerAngles;
    public Vector3 onEulerAngles = new Vector3(-35f, 0f, 0f);

    private bool switchedOn;

    private void Reset()
    {
        interactionPrompt = "Press F to Start Generator";
        interactionRadius = 2.5f;
    }

    public override string GetInteractionPrompt()
    {
        return switchedOn ? "Generator Running" : interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (switchedOn || puzzle == null)
            return;

        if (!puzzle.StartGenerator())
            return;

        switchedOn = true;
        ApplyHandleRotation();
    }

    private void ApplyHandleRotation()
    {
        if (switchHandle != null)
            switchHandle.localRotation = Quaternion.Euler(onEulerAngles);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && switchHandle != null)
            switchHandle.localRotation = Quaternion.Euler(offEulerAngles);
    }
}

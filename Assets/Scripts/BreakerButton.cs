using UnityEngine;

public class BreakerButton : InteractableBase
{
    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;
    public BreakerPanelRaycast breakerPanel;
    public int buttonIndex;

    [Header("Visuals")]
    public Material normalMaterial;
    public Material wrongMaterial;
    public Material correctMaterial;

    private Renderer buttonRenderer;
    private void Awake()
    {
        buttonRenderer = GetComponent<Renderer>();

        if (buttonRenderer != null && normalMaterial == null)
            normalMaterial = buttonRenderer.material;
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Press Breaker Button";
        interactionRadius = 2f;
    }

    public override string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (breakerPanel == null)
            breakerPanel = GetComponentInParent<BreakerPanelRaycast>();

        if (breakerPanel != null)
        {
            breakerPanel.Interact(player);
            return;
        }

        if (puzzle == null)
            return;

        puzzle.PressBreakerButton(buttonIndex);
    }

    private void SetMaterial(Material material)
    {
        if (buttonRenderer != null && material != null)
            buttonRenderer.material = material;
    }

    private void ResetMaterial()
    {
        SetMaterial(normalMaterial);
    }
}

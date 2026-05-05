using UnityEngine;

public class SimpleReadablePaper : InteractableBase
{
    [Header("UI Panel")]
    public GameObject paperPanel;

    [Header("Hint")]
    public string closeHint = "What happened here?";

    private bool hasBeenClosedOnce = false;
    private bool isOpen = false;

    private void Reset()
    {
        interactionPrompt = "Press F to Read";
        interactionRadius = 2f; // 🔥 IMPORTANT for radius system
    }

    public override string GetInteractionPrompt()
    {
        return isOpen ? "Press F to Close" : "Press F to Read";
    }

    public override void Interact(PlayerMovement player)
    {
        if (paperPanel == null)
        {
            Debug.LogWarning("Paper panel not assigned.");
            return;
        }

        if (isOpen)
        {
            ClosePaper(player);

            if (!hasBeenClosedOnce)
            {
                hasBeenClosedOnce = true;
                HintManager.Instance?.ShowHint(closeHint);
            }
        }
        else
        {
            OpenPaper(player);
        }
    }

    private void OpenPaper(PlayerMovement player)
    {
        paperPanel.SetActive(true);
        isOpen = true;

        if (player != null)
            player.SetReadingState(true);
    }

    private void ClosePaper(PlayerMovement player)
    {
        paperPanel.SetActive(false);
        isOpen = false;

        if (player != null)
            player.SetReadingState(false);
    }
}
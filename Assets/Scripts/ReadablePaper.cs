using UnityEngine;

public class ReadablePaper : InteractableBase
{
    [Header("Paper Content")]
    public string documentTitle = "Note";

    [TextArea(5, 25)]
    public string documentText;

    private void Reset()
    {
        interactionPrompt = "Press F to Read";
    }

    public override string GetInteractionPrompt()
    {
        if (DocumentUIManager.Instance != null && DocumentUIManager.Instance.IsShowing(this))
            return "Press F to Close";

        return "Press F to Read";
    }

    public override void Interact(PlayerMovement player)
    {
        if (DocumentUIManager.Instance == null)
        {
            Debug.LogWarning("No DocumentUIManager found in scene.");
            return;
        }

        if (DocumentUIManager.Instance.IsShowing(this))
        {
            DocumentUIManager.Instance.CloseDocument(player);
        }
        else
        {
            DocumentUIManager.Instance.OpenDocument(this, player);
        }
    }
}
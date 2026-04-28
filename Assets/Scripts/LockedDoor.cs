using UnityEngine;

public class LockedKeyDoor : InteractableBase
{
    [Header("Door")]
    public Door door;

    [Header("Key Requirement")]
    public string requiredKeyID = "Room2Key";

    [Header("Messages")]
    public string lockedMessage = "It's locked. I need a key.";
    public string unlockedMessage = "Unlocked.";

    private bool unlocked = false;

    private void Awake()
    {
        if (door == null)
            door = GetComponent<Door>() ?? GetComponentInParent<Door>();
    }

    public override string GetInteractionPrompt()
    {
        if (unlocked)
            return door != null && door.IsOpen ? "Press F to Close" : "Press F to Open";

        return "Press F to Unlock";
    }

    public override void Interact(PlayerMovement player)
    {
        if (door == null)
        {
            Debug.LogWarning("LockedKeyDoor has no Door assigned.");
            return;
        }

        if (!unlocked)
        {
            if (KeyInventory.Instance == null || !KeyInventory.Instance.HasKey(requiredKeyID))
            {
                HintManager.Instance?.ShowHint(lockedMessage);
                return;
            }

            unlocked = true;
            HintManager.Instance?.ShowHint(unlockedMessage);
        }

        door.Interact(player);
    }
}
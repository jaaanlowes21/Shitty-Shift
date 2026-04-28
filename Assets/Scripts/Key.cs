using UnityEngine;

public class KeyItem : InteractableBase
{
    [Header("Key Settings")]
    public string keyID = "Room2Key";
    public string pickupMessage = "Picked up a key.";

    public override string GetInteractionPrompt()
    {
        return "Press F to Pick Up Key";
    }

    public override void Interact(PlayerMovement player)
    {
        KeyInventory.Instance?.AddKey(keyID);
        HintManager.Instance?.ShowHint(pickupMessage);

        gameObject.SetActive(false);
    }
}
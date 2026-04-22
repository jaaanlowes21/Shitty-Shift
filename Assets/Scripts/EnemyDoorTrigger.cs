using UnityEngine;


[RequireComponent(typeof(Collider))]
public class EnemyDoorTrigger : MonoBehaviour
{
    [Header("Door")]
    [Tooltip("The door to open. Auto-detected from parent Door if left empty.")]
    public Door door;

    [Header("Settings")]
    [Tooltip("If true, the door only opens once and the trigger disables itself after.")]
    public bool openOnce = false;

    // ── Unity Messages ───────────────────────────────────────

    private void Awake()
    {
        // Auto-find the Door on the parent if not assigned manually
        if (door == null)
            door = GetComponentInParent<Door>();

        if (door == null)
            Debug.LogWarning($"EnemyDoorTrigger on {name} could not find a Door component.", this);

        // Make sure the collider is always a trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (door == null)
            return;

        // Only react to the enemy
        if (other.GetComponent<EnemyPatrol>() == null)
            return;

        // Only open if the door is currently closed
        if (!door.IsOpen)
            door.Interact(null);

        // Optionally disable after first use so it never interferes again
        if (openOnce)
            enabled = false;
    }
}

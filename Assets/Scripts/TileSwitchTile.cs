using UnityEngine;


public class TileSwitchTile : InteractableBase
{
    [Header("Tile Settings")]
    [Tooltip("Index of this tile (0 to totalTiles-1). Must be unique per tile.")]
    public int tileIndex = 0;

    [Tooltip("The puzzle manager that owns this tile.")]
    public TileSwitchPuzzle puzzleManager;

    [Header("Raycast Aim")]
    [Tooltip("If enabled, this tile only activates when the player is actually aiming at its collider.")]
    public bool requireRaycastAim = true;

    [Tooltip("Optional ray origin. Assign the FPS camera if this tile must be activated by looking directly at it.")]
    public Transform raycastSource;

    [Tooltip("Use Camera.main before falling back to the player's interact source.")]
    public bool preferMainCamera = true;

    public float raycastInteractionDistance = 4f;

    [Tooltip("Optional specific colliders that count as this tile. Leave empty to use this object's child colliders.")]
    public Collider[] raycastTileColliders;

    public bool drawRaycastDebug = false;

    [Header("Visuals")]
    [Tooltip("Material applied when this tile is correctly pressed.")]
    public Material pressedMaterial;

    [Tooltip("Material applied when this tile is pressed in wrong order.")]
    public Material wrongMaterial;

    private Material originalMaterial;
    private Renderer tileRenderer;
    private bool isPressed = false;

    // ── Unity Messages ───────────────────────────────────────

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();

        if (tileRenderer != null)
            originalMaterial = tileRenderer.material;
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Activate";
    }

    // ── InteractableBase Overrides ───────────────────────────

    public override string GetInteractionPrompt()
    {
        if (isPressed)
            return "Already Activated";

        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (isPressed || puzzleManager == null)
            return;

        if (requireRaycastAim && !IsAimingAtTile(player))
            return;

        puzzleManager.OnTilePressed(tileIndex);
    }

    // ── Visual Feedback ──────────────────────────────────────

    public void SetPressed()
    {
        isPressed = true;

        if (tileRenderer != null && pressedMaterial != null)
            tileRenderer.material = pressedMaterial;
    }

    public void SetWrong()
    {
        if (tileRenderer != null && wrongMaterial != null)
            tileRenderer.material = wrongMaterial;

        Invoke(nameof(ResetVisual), 0.5f);
    }

    public void ResetVisual()
    {
        isPressed = false;

        if (tileRenderer != null && originalMaterial != null)
            tileRenderer.material = originalMaterial;
    }

    private bool IsAimingAtTile(PlayerMovement player)
    {
        Transform source = GetRaycastSource(player);
        if (source == null)
            return false;

        if (drawRaycastDebug)
            Debug.DrawRay(source.position, source.forward * raycastInteractionDistance, Color.magenta, 0.1f);

        RaycastHit[] hits = Physics.RaycastAll(
            source.position,
            source.forward,
            raycastInteractionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return false;

        foreach (RaycastHit hit in hits)
        {
            if (IsTileCollider(hit.collider))
                return true;
        }

        return false;
    }

    private Transform GetRaycastSource(PlayerMovement player)
    {
        if (raycastSource != null)
            return raycastSource;

        if (preferMainCamera && Camera.main != null)
            return Camera.main.transform;

        if (player != null && player.interactSource != null)
            return player.interactSource;

        return player != null ? player.transform : transform;
    }

    private bool IsTileCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (raycastTileColliders != null && raycastTileColliders.Length > 0)
        {
            foreach (Collider tileCollider in raycastTileColliders)
            {
                if (tileCollider == null)
                    continue;

                if (hitCollider == tileCollider || hitCollider.transform.IsChildOf(tileCollider.transform))
                    return true;
            }

            return false;
        }

        return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
    }
}

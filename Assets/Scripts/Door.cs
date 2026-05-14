using System.Collections;
using UnityEngine;

public class Door : InteractableBase
{
    [Header("Door Rotation")]
    public float openAngle = 90f;
    public float rotateSpeed = 3f;
    public bool startOpen = false;

    public enum RotationAxis { X, Y, Z }
    public RotationAxis axis = RotationAxis.Z; 

    [Header("Closed Door")]
    public bool isClosedDoor = false;

    [Header("Raycast Aim")]
    [Tooltip("If enabled, radius interaction will only work when the player is actually aiming at this door.")]
    public bool requireRaycastAim = false;

    [Tooltip("Optional ray origin. Assign the FPS camera if this door must be opened by looking directly at it.")]
    public Transform raycastSource;

    [Tooltip("Use Camera.main before falling back to the player's interact source.")]
    public bool preferMainCamera = true;

    public float raycastInteractionDistance = 3.5f;

    [Tooltip("Optional specific colliders that count as this door. Leave empty to use this object's child colliders.")]
    public Collider[] raycastDoorColliders;

    public bool drawRaycastDebug = false;

    [Header("Auto Close")]
    public bool autoClose = true;
    public float autoCloseDelay = 3f;

    [Header("Door Sounds")]
    public AudioClip openSound;
    [Range(0f, 1f)] public float openVolume = 0.7f;
    
    public AudioClip closeSound;
    [Range(0f, 1f)] public float closeVolume = 0.7f;
    
    public AudioClip lockedSound; // Sound when trying to open a locked/closed door
    [Range(0f, 1f)] public float lockedVolume = 0.8f;

    private const float RotationTolerance = 0.1f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isMoving;

    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;

    private Coroutine rotateCoroutine;
    private Coroutine autoCloseCoroutine;
    private AudioSource audioSource;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;

        closedRotation = transform.localRotation;

        Vector3 euler = closedRotation.eulerAngles;

        switch (axis)
        {
            case RotationAxis.X:
                euler.x += openAngle;
                break;
            case RotationAxis.Y:
                euler.y += openAngle;
                break;
            case RotationAxis.Z:
                euler.z += openAngle;
                break;
        }

        openRotation = Quaternion.Euler(euler);

        isOpen = startOpen;

        transform.localRotation = isOpen ? openRotation : closedRotation;
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Open";
        interactionRadius = 2.5f; 
    }

    private void OnDisable()
    {
        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);
    }

    public override string GetInteractionPrompt()
    {
        if (isMoving)
            return "Door Moving...";

        return isOpen ? "Press F to Close" : "Press F to Open";
    }

    public override void Interact(PlayerMovement player)
    {
        if (requireRaycastAim && !IsAimingAtDoor(player))
            return;

        LockedKeyDoor lockedDoor = GetComponent<LockedKeyDoor>();

        if (lockedDoor != null && !lockedDoor.IsUnlocked)
        {
            lockedDoor.Interact(player);
            return;
        }

        if (isMoving)
            return;

        if (isClosedDoor && !isOpen)
        {
            HintManager.Instance?.ShowHint("Weird, the doors are locked... I should try the other side");
            PlaySound(lockedSound, lockedVolume);
            return;
        }

        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        isOpen = !isOpen;

        // Play appropriate sound
        if (isOpen)
            PlaySound(openSound, openVolume);
        else
            PlaySound(closeSound, closeVolume);

        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        rotateCoroutine = StartCoroutine(RotateDoor(targetRotation));

        if (isOpen && autoClose)
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }

    public void ForceOpen()
    {
        if (isOpen || isMoving) return;

        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        isOpen = true;
        PlaySound(openSound, openVolume);
        rotateCoroutine = StartCoroutine(RotateDoor(openRotation));

        if (autoClose)
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        isMoving = true;

        while (Quaternion.Angle(transform.localRotation, targetRotation) > RotationTolerance)
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                rotateSpeed * 100f * Time.deltaTime
            );

            yield return null;
        }

        transform.localRotation = targetRotation;
        isMoving = false;
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (!isOpen || isMoving)
            yield break;

        isOpen = false;
        PlaySound(closeSound, closeVolume);
        rotateCoroutine = StartCoroutine(RotateDoor(closedRotation));
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    private bool IsAimingAtDoor(PlayerMovement player)
    {
        Transform source = GetRaycastSource(player);
        if (source == null)
            return false;

        if (drawRaycastDebug)
            Debug.DrawRay(source.position, source.forward * raycastInteractionDistance, Color.green, 0.1f);

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
            if (IsDoorCollider(hit.collider))
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

    private bool IsDoorCollider(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (raycastDoorColliders != null && raycastDoorColliders.Length > 0)
        {
            foreach (Collider doorCollider in raycastDoorColliders)
            {
                if (doorCollider == null)
                    continue;

                if (hitCollider == doorCollider || hitCollider.transform.IsChildOf(doorCollider.transform))
                    return true;
            }

            return false;
        }

        return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
    }
}

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

    [Header("Auto Close")]
    public bool autoClose = true;
    public float autoCloseDelay = 3f;

    private const float RotationTolerance = 0.1f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isMoving;

    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;

    private Coroutine rotateCoroutine;
    private Coroutine autoCloseCoroutine;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

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
        interactionRadius = 2.5f; // 🔥 works with your radius system
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
        return;
    }

    if (rotateCoroutine != null)
        StopCoroutine(rotateCoroutine);

    if (autoCloseCoroutine != null)
        StopCoroutine(autoCloseCoroutine);

    isOpen = !isOpen;

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
        rotateCoroutine = StartCoroutine(RotateDoor(closedRotation));
    }
}
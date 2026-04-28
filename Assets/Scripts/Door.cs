using System.Collections;
using UnityEngine;

public class Door : InteractableBase
{
    [Header("Door Rotation")]
    public float openAngle = 90f;
    public float rotateSpeed = 3f;
    public bool startOpen = false;

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

    private Coroutine rotateCoroutine;
    private Coroutine autoCloseCoroutine;

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(
            closedRotation.eulerAngles.x,
            closedRotation.eulerAngles.y + openAngle,
            closedRotation.eulerAngles.z
        );

        isOpen = startOpen;

        transform.localRotation = isOpen ? openRotation : closedRotation;
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Open";
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
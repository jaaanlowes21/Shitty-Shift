using System.Collections;
using UnityEngine;

public class Door : InteractableBase
{
    [Header("Door Settings")]
    public float openDistance = 2f;
    public float slideSpeed = 3f;
    public bool useLocalSpace = true;
    public bool startOpen = false;

    [Header("Auto Close")]
    public bool autoClose = true;
    public float autoCloseDelay = 3f;

    private const float PositionTolerance = 0.001f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private bool isMoving;
    public bool IsOpen => isOpen;

    private Coroutine moveCoroutine;
    private Coroutine autoCloseCoroutine;

    private void Awake()
    {
        if (useLocalSpace)
        {
            closedPosition = transform.localPosition;
            openPosition = closedPosition + Vector3.right * openDistance;
        }
        else
        {
            closedPosition = transform.position;
            openPosition = closedPosition + transform.right * openDistance;
        }

        isOpen = startOpen;

        if (isOpen)
        {
            if (useLocalSpace)
                transform.localPosition = openPosition;
            else
                transform.position = openPosition;
        }
        else
        {
            if (useLocalSpace)
                transform.localPosition = closedPosition;
            else
                transform.position = closedPosition;
        }
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Open";
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

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

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        isOpen = !isOpen;

        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        moveCoroutine = StartCoroutine(SlideDoor(targetPosition));

        if (isOpen && autoClose)
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }

    private IEnumerator SlideDoor(Vector3 targetPosition)
    {
        isMoving = true;

        while (true)
        {
            Vector3 currentPosition = useLocalSpace ? transform.localPosition : transform.position;

            currentPosition = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                slideSpeed * Time.deltaTime
            );

            if (useLocalSpace)
                transform.localPosition = currentPosition;
            else
                transform.position = currentPosition;

            if (Vector3.Distance(currentPosition, targetPosition) < PositionTolerance)
                break;

            yield return null;
        }

        if (useLocalSpace)
            transform.localPosition = targetPosition;
        else
            transform.position = targetPosition;

        isMoving = false;
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (!isOpen || isMoving)
            yield break;

        isOpen = false;
        moveCoroutine = StartCoroutine(SlideDoor(closedPosition));
    }
}
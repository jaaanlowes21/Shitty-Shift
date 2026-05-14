using System.Collections;
using UnityEngine;

public class PowerLever : InteractableBase
{
    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;

    [Header("Visuals")]
    public Transform leverHandle;
    public Vector3 offEulerAngles;
    public Vector3 onEulerAngles = new Vector3(-60f, 0f, 0f);
    public float rotateSpeed = 8f;
    public bool returnToOffWhenWrong = true;

    [Header("Prompts")]
    public string noPowerPrompt = "Needs Generator Power";
    public string chooseSwitchPrompt = "Choose a Switch First";
    public string testPowerPrompt = "Press F to Test Power";
    public string restoredPrompt = "Power Restored";

    private bool pulled;
    private bool isMoving;
    private Coroutine rotateCoroutine;

    private void Reset()
    {
        interactionPrompt = "Press F to Test Power";
        interactionRadius = 2.5f;
    }

    public override string GetInteractionPrompt()
    {
        if (isMoving)
            return "Testing Power...";

        if (pulled || (puzzle != null && puzzle.IsPowerRestored))
            return restoredPrompt;

        if (puzzle == null)
            return interactionPrompt;

        if (puzzle.currentState < ElectricalPowerPuzzle.ElectricalState.GeneratorRunning)
            return noPowerPrompt;

        if (!puzzle.HasSelectedBreakerButton)
            return chooseSwitchPrompt;

        return testPowerPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (pulled || isMoving || puzzle == null)
            return;

        bool shouldAnimateTest = puzzle.currentState == ElectricalPowerPuzzle.ElectricalState.GeneratorRunning &&
                                 puzzle.HasSelectedBreakerButton;

        bool restoredPower = puzzle.PullLever();

        if (!restoredPower)
        {
            if (shouldAnimateTest)
                StartLeverAnimation(false);

            return;
        }

        pulled = true;
        StartLeverAnimation(true);
    }

    private void StartLeverAnimation(bool stayOn)
    {
        if (leverHandle == null)
            return;

        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        rotateCoroutine = StartCoroutine(RotateLeverRoutine(stayOn));
    }

    private IEnumerator RotateLeverRoutine(bool stayOn)
    {
        isMoving = true;

        yield return RotateHandle(Quaternion.Euler(onEulerAngles));

        if (!stayOn && returnToOffWhenWrong)
            yield return RotateHandle(Quaternion.Euler(offEulerAngles));

        isMoving = false;
        rotateCoroutine = null;
    }

    private IEnumerator RotateHandle(Quaternion targetRotation)
    {
        while (Quaternion.Angle(leverHandle.localRotation, targetRotation) > 0.1f)
        {
            leverHandle.localRotation = Quaternion.Slerp(
                leverHandle.localRotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );

            yield return null;
        }

        leverHandle.localRotation = targetRotation;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && leverHandle != null)
            leverHandle.localRotation = Quaternion.Euler(offEulerAngles);
    }
}

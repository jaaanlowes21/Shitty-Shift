using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SafeLock : InteractableBase
{
    [Header("Safe Code")]
    public string correctCode = "1234";

    [Header("Safe UI")]
    public GameObject safePanel;
    public TextMeshProUGUI[] digitDisplays = new TextMeshProUGUI[4];
    public TextMeshProUGUI statusText;

    [Header("Reward Key")]
    public GameObject rewardObject;

    [Header("Messages")]
    public string unlockMessage = "Safe unlocked.";
    public string wrongCodeMessage = "Wrong code.";

    [Header("Safe Door Pivot")]
    public Transform safeDoorPivot;
    public float doorOpenAngle = 120f;
    public float doorRotateSpeed = 1.75f;

    [Header("Settings")]
    public bool showWrongCodeHint = true;

    private const int CombinationLength = 4;

    private static readonly Key[] digitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    private static readonly Key[] numpadKeys =
    {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };

    private char[] enteredDigits = new char[CombinationLength] { '0', '0', '0', '0' };
    private int currentSlotIndex = 0;

    private bool isOpen = false;
    private bool isDoorRotating = false;
    private PlayerMovement currentPlayer;

    private void Awake()
    {
        if (rewardObject != null)
            rewardObject.SetActive(false);
    }

    private void Start()
    {
        if (safePanel != null)
            safePanel.SetActive(false);

        if (rewardObject != null)
            rewardObject.SetActive(false);

        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("");
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Use Safe";
        interactionRadius = 5f;
    }

    private void OnValidate()
    {
        if (correctCode == null)
            correctCode = "0000";

        correctCode = new string(correctCode.Where(char.IsDigit).ToArray());

        if (correctCode.Length < 4)
            correctCode = correctCode.PadRight(4, '0');
        else if (correctCode.Length > 4)
            correctCode = correctCode.Substring(0, 4);
    }

    private void Update()
    {
        if (safePanel == null || !safePanel.activeSelf || currentPlayer == null)
            return;

        ReadDigitInput();
    }

    public override string GetInteractionPrompt()
    {
        return isOpen ? "Safe Opened" : "Press F to Use Safe";
    }

    public override void Interact(PlayerMovement player)
    {
        if (isOpen)
        {
            HintManager.Instance?.ShowHint("The safe is already open.");
            return;
        }

        currentPlayer = player;

        if (safePanel != null)
            safePanel.SetActive(true);

        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("");

        if (currentPlayer != null)
            currentPlayer.SetReadingState(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ReadDigitInput()
    {
        if (Keyboard.current == null)
            return;

        if (TryReadDigitKey(out char digit))
        {
            if (currentSlotIndex < enteredDigits.Length)
            {
                enteredDigits[currentSlotIndex] = digit;
                currentSlotIndex++;
                UpdateDigitDisplays();
            }

            if (currentSlotIndex >= enteredDigits.Length)
                ValidateCombination();
        }
        else if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            Backspace();
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseSafeUI();
        }
    }

    private bool TryReadDigitKey(out char digit)
    {
        digit = default;

        if (Keyboard.current == null)
            return false;

        for (int i = 0; i < digitKeys.Length; i++)
        {
            if (Keyboard.current[digitKeys[i]].wasPressedThisFrame ||
                Keyboard.current[numpadKeys[i]].wasPressedThisFrame)
            {
                digit = (char)('0' + i);
                return true;
            }
        }

        return false;
    }

    public void InputNumber(string number)
    {
        if (isOpen || string.IsNullOrEmpty(number) || !char.IsDigit(number[0]))
            return;

        if (currentSlotIndex < enteredDigits.Length)
        {
            enteredDigits[currentSlotIndex] = number[0];
            currentSlotIndex++;
            UpdateDigitDisplays();

            if (currentSlotIndex >= enteredDigits.Length)
                ValidateCombination();
        }
    }

    public void Backspace()
    {
        if (isOpen)
            return;

        if (currentSlotIndex > 0)
        {
            currentSlotIndex--;
            enteredDigits[currentSlotIndex] = '0';
            UpdateDigitDisplays();
        }
    }

    public void ClearInput()
    {
        if (isOpen)
            return;

        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("");
    }

    public void SubmitCode()
    {
        if (isOpen)
            return;

        ValidateCombination();
    }

    private void ValidateCombination()
    {
        string entered = new string(enteredDigits);

        if (entered == correctCode)
        {
            OpenSafe();
            return;
        }

        if (showWrongCodeHint)
        {
            UpdateStatusText(wrongCodeMessage);
            HintManager.Instance?.ShowHint(wrongCodeMessage);
        }

        ResetEnteredDigits();
        UpdateDigitDisplays();
    }

    private void OpenSafe()
    {
        isOpen = true;

        CloseSafeUI();

        if (rewardObject != null)
            rewardObject.SetActive(true);

        if (safeDoorPivot != null && !isDoorRotating)
            StartCoroutine(RotateSafeDoor());

        HintManager.Instance?.ShowHint(unlockMessage);
    }

    private IEnumerator RotateSafeDoor()
    {
        isDoorRotating = true;

        Quaternion startRotation = safeDoorPivot.localRotation;

        Quaternion targetRotation = Quaternion.Euler(
            startRotation.eulerAngles.x,
            startRotation.eulerAngles.y + doorOpenAngle,
            startRotation.eulerAngles.z
        );

        while (Quaternion.Angle(safeDoorPivot.localRotation, targetRotation) > 0.1f)
        {
            safeDoorPivot.localRotation = Quaternion.RotateTowards(
                safeDoorPivot.localRotation,
                targetRotation,
                doorRotateSpeed * 100f * Time.deltaTime
            );

            yield return null;
        }

        safeDoorPivot.localRotation = targetRotation;
        isDoorRotating = false;
    }

    public void CloseSafeUI()
    {
        if (safePanel != null)
            safePanel.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.SetReadingState(false);

        currentPlayer = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResetEnteredDigits()
    {
        for (int i = 0; i < enteredDigits.Length; i++)
            enteredDigits[i] = '0';

        currentSlotIndex = 0;
    }

    private void UpdateDigitDisplays()
    {
        for (int i = 0; i < digitDisplays.Length; i++)
        {
            if (digitDisplays[i] != null)
                digitDisplays[i].text = enteredDigits[i].ToString();
        }
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
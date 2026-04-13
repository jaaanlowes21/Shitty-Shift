using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CombinationLockDoor : Door
{
    [Header("Combination Lock")]
    [Tooltip("Four digit combination to unlock the door.")]
    public string combination = "1234";

    [Header("UI")]
    public GameObject combinationPanel;
    public TextMeshProUGUI[] digitDisplays = new TextMeshProUGUI[4];
    public TextMeshProUGUI statusText;

    private char[] enteredDigits = new char[4] { '0', '0', '0', '0' };
    private int currentSlotIndex = 0;
    private bool panelOpen = false;
    private bool isUnlocked = false;
    private PlayerMovement currentPlayer;

    private void OnValidate()
    {
        if (combination == null)
            combination = "0000";

        combination = new string(combination.Where(char.IsDigit).ToArray());

        if (combination.Length < 4)
            combination = combination.PadRight(4, '0');
        else if (combination.Length > 4)
            combination = combination.Substring(0, 4);
    }

    private void Start()
    {
        ResetEnteredDigits();
        HidePanel();
    }

    private void Update()
    {
        if (!panelOpen || currentPlayer == null)
            return;

        ReadDigitInput();
    }

    public override string GetInteractionPrompt()
    {
        if (panelOpen)
            return "Press F to Close";

        if (isUnlocked)
            return base.GetInteractionPrompt();

        return "Press F to Enter Code";
    }

    public override void Interact(PlayerMovement player)
    {
        if (isUnlocked)
        {
            base.Interact(player);
            return;
        }

        if (panelOpen)
        {
            ClosePanel();
            return;
        }

        OpenPanel(player);
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
            if (currentSlotIndex > 0)
            {
                currentSlotIndex--;
                enteredDigits[currentSlotIndex] = '0';
                UpdateDigitDisplays();
                UpdateStatusText("Backspace");
            }
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    private bool TryReadDigitKey(out char digit)
    {
        digit = '0';

        if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame)
            digit = '0';
        else if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            digit = '1';
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            digit = '2';
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            digit = '3';
        else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
            digit = '4';
        else if (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame)
            digit = '5';
        else if (Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame)
            digit = '6';
        else if (Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame)
            digit = '7';
        else if (Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame)
            digit = '8';
        else if (Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame)
            digit = '9';
        else
            return false;

        return true;
    }

    private void ValidateCombination()
    {
        string entered = new string(enteredDigits);

        if (entered == combination)
        {
            isUnlocked = true;
            UpdateStatusText("Unlocked!");
            ClosePanel();
            base.Interact(currentPlayer);
        }
        else
        {
            UpdateStatusText("Wrong code. Try again.");
            ResetEnteredDigits();
        }
    }

    private void OpenPanel(PlayerMovement player)
    {
        currentPlayer = player;
        panelOpen = true;

        if (combinationPanel != null)
            combinationPanel.SetActive(true);

        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("Enter 4 digits. Backspace to delete.");

        if (currentPlayer != null)
            currentPlayer.SetReadingState(true);
    }

    private void ClosePanel()
    {
        panelOpen = false;

        if (combinationPanel != null)
            combinationPanel.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.SetReadingState(false);

        currentPlayer = null;
    }

    private void HidePanel()
    {
        if (combinationPanel != null)
            combinationPanel.SetActive(false);
    }

    private void ResetEnteredDigits()
    {
        for (int i = 0; i < enteredDigits.Length; i++)
            enteredDigits[i] = '0';

        currentSlotIndex = 0;
    }

    private void UpdateDigitDisplays()
    {
        if (digitDisplays == null)
            return;

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
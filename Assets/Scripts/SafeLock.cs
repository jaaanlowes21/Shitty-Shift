using UnityEngine;
using TMPro;

public class SafeLock : InteractableBase
{
    [Header("Safe Code")]
    public string correctCode = "1234";
    public int codeLength = 4;

    [Header("Safe UI")]
    public GameObject safePanel;
    public TextMeshProUGUI inputText;
    public TextMeshProUGUI feedbackText;

    [Header("Reward")]
    public GameObject rewardObject;
    public string unlockMessage = "Safe unlocked.";
    public string wrongCodeMessage = "Wrong code.";

    [Header("Settings")]
    public bool allowUnlimitedAttempts = true;
    public bool showWrongCodeHint = true;

    private string currentInput = "";
    private bool isOpen = false;
    private PlayerMovement currentPlayer;

    private void Start()
    {
        if (safePanel != null)
            safePanel.SetActive(false);

        if (rewardObject != null)
            rewardObject.SetActive(false);

        UpdateInputUI();
        SetFeedback("");
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

        currentInput = "";
        UpdateInputUI();
        SetFeedback("");

        if (currentPlayer != null)
            currentPlayer.SetReadingState(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void InputNumber(string number)
    {
        if (isOpen)
            return;

        if (currentInput.Length >= codeLength)
            return;

        currentInput += number;
        UpdateInputUI();
    }

    public void ClearInput()
    {
        currentInput = "";
        UpdateInputUI();
        SetFeedback("");
    }

    public void Backspace()
    {
        if (currentInput.Length <= 0)
            return;

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdateInputUI();
    }

    public void SubmitCode()
    {
        if (isOpen)
            return;

        if (currentInput == correctCode)
        {
            OpenSafe();
            return;
        }

        if (showWrongCodeHint)
        {
            SetFeedback(wrongCodeMessage);
            HintManager.Instance?.ShowHint(wrongCodeMessage);
        }

        currentInput = "";
        UpdateInputUI();
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

    private void OpenSafe()
    {
        isOpen = true;

        if (rewardObject != null)
            rewardObject.SetActive(true);

        HintManager.Instance?.ShowHint(unlockMessage);

        CloseSafeUI();
    }

    private void UpdateInputUI()
    {
        if (inputText != null)
            inputText.text = currentInput;
    }

    private void SetFeedback(string text)
    {
        if (feedbackText != null)
            feedbackText.text = text;
    }
}
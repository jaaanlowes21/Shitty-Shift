using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class CombinationLockDoor : InteractableBase
{
    [Header("Combination Lock")]
    public string combination = "1234";

    [Header("Scene To Load")]
    public string sceneName = "GameScene";

    [Header("Exit Door (End Game)")]
    [Tooltip("Check this if this is the final door that ends the game")]
    public bool isExitDoor = false;

    [Header("UI")]
    public GameObject combinationPanel;
    public TextMeshProUGUI[] digitDisplays = new TextMeshProUGUI[4];
    public TextMeshProUGUI statusText;

    [Header("Fade Transition")]
    public Image blackFadeImage;
    public float fadeDuration = 5f;

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
    private int currentSlotIndex;
    private bool panelOpen;
    private PlayerMovement currentPlayer;

    private void Reset()
    {
        interactionPrompt = "Press F to Enter Code";
        interactionRadius = 2.5f;
    }

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

        if (combinationPanel != null)
            combinationPanel.SetActive(false);
    }

    private void Update()
    {
        if (!panelOpen || currentPlayer == null)
            return;

        ReadDigitInput();
    }

    public override string GetInteractionPrompt()
    {
        return panelOpen ? "Press F to Close" : "Press F to Enter Code";
    }

    public override void Interact(PlayerMovement player)
    {
        if (panelOpen)
            ClosePanel();
        else
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
            Backspace();
            UpdateStatusText("Backspace");
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
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

    private void ValidateCombination()
    {
        string entered = new string(enteredDigits);

        if (entered == combination)
        {
            UpdateStatusText("Unlocked!");
            ClosePanel();
            
            // Stop timer if this is the exit door
            if (isExitDoor)
            {
                StopGameTimer();
            }
            
            StartCoroutine(LoadSceneWithFade());
            return;
        }

        UpdateStatusText("Wrong code. Try again.");
        ResetEnteredDigits();
        UpdateDigitDisplays();
    }

    private void StopGameTimer()
    {
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
            Debug.Log($"[CombinationLockDoor] Exit door opened! Timer stopped at: {GameTimer.Instance.GetFormattedTime()}");
        }
        else
        {
            Debug.LogWarning("[CombinationLockDoor] GameTimer Instance not found!");
        }
    }

    public void InputNumber(string number)
    {
        if (string.IsNullOrEmpty(number) || !char.IsDigit(number[0]))
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
        if (currentSlotIndex > 0)
        {
            currentSlotIndex--;
            enteredDigits[currentSlotIndex] = '0';
            UpdateDigitDisplays();
        }
    }

    public void ClearInput()
    {
        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("");
    }

    public void SubmitCode()
    {
        ValidateCombination();
    }

    private void OpenPanel(PlayerMovement player)
    {
        currentPlayer = player;
        panelOpen = true;

        if (combinationPanel != null)
            combinationPanel.SetActive(true);

        ResetEnteredDigits();
        UpdateDigitDisplays();
        UpdateStatusText("Enter 4 digits.");

        if (currentPlayer != null)
            currentPlayer.SetReadingState(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePanel()
    {
        panelOpen = false;

        if (combinationPanel != null)
            combinationPanel.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.SetReadingState(false);

        currentPlayer = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (panelOpen)
            ClosePanel();
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

    private IEnumerator LoadSceneWithFade()
    {
        // Pause everything
        Time.timeScale = 0f;

        if (blackFadeImage != null)
            blackFadeImage.gameObject.SetActive(true);

        // Wait 3 seconds (unscaled time)
        yield return new WaitForSecondsRealtime(3f);

        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }
}
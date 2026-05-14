using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    [Header("Timing")]
    public float typewriterDelay = 0.03f;
    public float lineHoldDuration = 1.5f;
    public bool allowSkipLine = true;

    private Coroutine dialogueCoroutine;
    private bool skipLineRequested;
    private PlayerMovement lockedPlayer;

    public bool IsPlaying => dialogueCoroutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!allowSkipLine || !IsPlaying)
            return;

        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.fKey.wasPressedThisFrame))
        {
            skipLineRequested = true;
        }
    }

    public void PlayDialogue(string[] lines, string speaker = "", bool lockPlayer = false, PlayerMovement player = null, UnityEvent onComplete = null)
    {
        if (lines == null || lines.Length == 0)
            return;

        if (dialogueText == null)
        {
            HintManager.Instance?.ShowHint(lines[0]);
            onComplete?.Invoke();
            return;
        }

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(PlayDialogueRoutine(lines, speaker, lockPlayer, player, onComplete));
    }

    private IEnumerator PlayDialogueRoutine(string[] lines, string speaker, bool lockPlayer, PlayerMovement player, UnityEvent onComplete)
    {
        lockedPlayer = lockPlayer ? player : null;

        if (lockedPlayer != null)
            lockedPlayer.SetReadingState(true);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (speakerText != null)
        {
            speakerText.text = speaker;
            speakerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(speaker));
        }

        foreach (string line in lines)
        {
            yield return TypeLine(line);

            skipLineRequested = false;
            float timer = 0f;
            while (timer < lineHoldDuration && !skipLineRequested)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (lockedPlayer != null)
        {
            lockedPlayer.SetReadingState(false);
            lockedPlayer = null;
        }

        dialogueCoroutine = null;
        skipLineRequested = false;
        onComplete?.Invoke();
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = string.Empty;
        skipLineRequested = false;

        string safeLine = line ?? string.Empty;
        foreach (char character in safeLine)
        {
            if (skipLineRequested)
            {
                dialogueText.text = safeLine;
                skipLineRequested = false;
                yield break;
            }

            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typewriterDelay);
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SecondFloorManager : MonoBehaviour
{
    public static bool IsSecondFloorIntroActive { get; private set; }

    [Header("Intro UI")]
    public GameObject introPanel;
    public Image backgroundFadeImage;
    public TextMeshProUGUI narrationText;

    [Header("Start Hints")]
    public string[] startHints =
    {
        "What's wrong here?",
        "Why is it messier than the third floor?",
        "I better be careful in case another monster is here."
    };

    public float delayBeforeFirstHint = 1f;
    public float timeBetweenHints = 3f;
    public float textFadeDuration = 0.6f;

    [Header("Background Fade")]
    public float backgroundFadeDuration = 1f;

    private Coroutine introCoroutine;
    private bool skipRequested;

    private void Awake()
    {
        IsSecondFloorIntroActive = true;

        // Lock cursor during intro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (introPanel != null)
            introPanel.SetActive(true);

        if (backgroundFadeImage != null)
        {
            // Set initial color with full alpha for black screen
            Color color = backgroundFadeImage.color;
            color.a = 1f;
            backgroundFadeImage.color = color;
        }

        if (narrationText != null)
        {
            narrationText.text = string.Empty;
            // Start with invisible text
            Color color = narrationText.color;
            color.a = 0f;
            narrationText.color = color;
        }

        Time.timeScale = 0f; // Pause the game
    }

    private void Start()
    {
        introCoroutine = StartCoroutine(RunIntro());
    }

    private void Update()
    {
        if (!IsSecondFloorIntroActive)
            return;

        // Skip intro with ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SkipIntro();
    }

    public void SkipIntro()
    {
        if (!IsSecondFloorIntroActive)
            return;

        skipRequested = true;
        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        StartCoroutine(CompleteIntro());
    }

    private IEnumerator RunIntro()
    {
        // Fade background from black to clear
        yield return FadeImageAlpha(backgroundFadeImage, 1f, 0f, backgroundFadeDuration);

        if (skipRequested)
        {
            yield return CompleteIntro();
            yield break;
        }

        // Wait before first hint
        yield return new WaitForSecondsRealtime(delayBeforeFirstHint);

        // Show each hint
        foreach (string hint in startHints)
        {
            if (skipRequested)
                break;

            if (narrationText != null)
            {
                // Set text
                narrationText.text = hint;
                
                // Fade text in
                yield return FadeTextAlpha(0f, 1f, textFadeDuration);
                
                if (skipRequested) break;
                
                // Hold the text on screen
                yield return new WaitForSecondsRealtime(timeBetweenHints);
                
                if (skipRequested) break;
                
                // Fade text out
                yield return FadeTextAlpha(1f, 0f, textFadeDuration);
                
                // Clear text
                narrationText.text = string.Empty;
            }
        }

        if (!skipRequested)
            yield return CompleteIntro();
    }

    private IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (narrationText == null)
            yield break;

        float timer = 0f;
        Color color = narrationText.color;

        while (timer < duration && !skipRequested)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, timer / Mathf.Max(duration, 0.0001f));
            narrationText.color = color;
            yield return null;
        }

        color.a = to;
        narrationText.color = color;
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        float timer = 0f;
        Color color = image.color;
        color.a = from;
        image.color = color;

        while (timer < duration && !skipRequested)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, timer / Mathf.Max(duration, 0.0001f));
            image.color = color;
            yield return null;
        }

        color.a = to;
        image.color = color;
    }

    private IEnumerator CompleteIntro()
    {
        // Make sure background is fully transparent
        if (backgroundFadeImage != null)
        {
            Color color = backgroundFadeImage.color;
            color.a = 0f;
            backgroundFadeImage.color = color;
        }

        if (narrationText != null)
            narrationText.text = string.Empty;

        if (introPanel != null)
            introPanel.SetActive(false);

        IsSecondFloorIntroActive = false;
        skipRequested = false;
        Time.timeScale = 1f; // Resume the game

        // Keep cursor locked for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HintManager.Instance?.ShowHint("The Air Here is Weird...");

        yield return null;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;


public class TileSwitchPuzzle : MonoBehaviour
{
    [Header("Tiles")]
    [Tooltip("All TileSwitchTile components in this puzzle. Order does not matter here.")]
    public TileSwitchTile[] tiles;

    [Header("Sequence")]
    [Tooltip("The correct order of tile indices the player must press. " +
             "e.g. {2, 0, 3, 1} = press tile 2 first, then 0, then 3, then 1.")]
    public int[] correctSequence;

    [Header("Clue Reveal")]
    [Tooltip("UI Panel shown when the puzzle is solved.")]
    public GameObject cluePanel;

    [Tooltip("TMP text that displays the revealed clue or code.")]
    public TextMeshProUGUI clueText;

    [Tooltip("The clue message revealed on solve. " +
             "e.g. 'The first digit is 4...' or 'Code: 4???'")]
    [TextArea(2, 4)]
    public string clueMessage = "Clue: ???";

    [Header("Clue Object")]
    [Tooltip("Physical clue object to reveal when the puzzle is solved.")]
    public GameObject clueObject;

    [Tooltip("Optional readable paper component on the clue object.")]
    public ReadablePaper cluePaper;

    [Tooltip("Title shown when the clue note is opened.")]
    public string clueTitle = "Note";

    [Header("UI")]
    [Tooltip("TMP text showing puzzle progress and feedback to the player.")]
    public TextMeshProUGUI statusText;

    [Header("Feedback Text")]
    public string startMessage = "Press the colors in the correct order.";
    public string correctStepMessage = "Correct color. Keep going.";
    public string wrongStepMessage = "Wrong color. The sequence reset.";
    public string solvedMessage = "Color sequence solved.";
    public float wrongResetDelay = 0.6f;

    [Header("Objective Guide")]
    public bool updateObjectiveGuide = true;
    public string startingObjective = "Complete the color sequence.";
    public string solvedObjective = "Return to the security terminal.";

    [Header("Audio")]
    public AudioClip correctTileSound;
    public AudioClip wrongTileSound;
    public AudioClip solvedSound;

    [Header("Completion")]
    [Tooltip("Invoked once after the tile switch puzzle is solved.")]
    public UnityEvent onPuzzleSolved;

    private int currentStep = 0;
    private bool isSolved = false;

    private AudioSource audioSource;

    // ── Unity Messages ───────────────────────────────────────

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        HideCluePanel();
        HideClueObject();
        UpdateStatus(startMessage);

        if (updateObjectiveGuide)
            ObjectiveManager.Instance?.SetObjective(startingObjective);
    }

    // ── Called by TileSwitchTile ─────────────────────────────

    public void OnTilePressed(int tileIndex)
    {
        if (isSolved)
            return;

        if (IntroManager.IsIntroActive)
            return;

        if (currentStep >= correctSequence.Length)
            return;

        if (tileIndex == correctSequence[currentStep])
        {
            HandleCorrectTile(tileIndex);
        }
        else
        {
            HandleWrongTile(tileIndex);
        }
    }

    // ── Correct / Wrong Handling ─────────────────────────────

    private void HandleCorrectTile(int tileIndex)
    {
        PlaySound(correctTileSound);

        TileSwitchTile tile = GetTileByIndex(tileIndex);
        if (tile != null)
            tile.SetPressed();

        currentStep++;

        if (currentStep >= correctSequence.Length)
        {
            StartCoroutine(SolvePuzzle());
        }
        else
        {
            UpdateStatus($"{correctStepMessage} Step {currentStep + 1} of {correctSequence.Length}.");
        }
    }

    private void HandleWrongTile(int tileIndex)
    {
        PlaySound(wrongTileSound);

        TileSwitchTile wrongTile = GetTileByIndex(tileIndex);
        if (wrongTile != null)
            wrongTile.SetWrong();

        StartCoroutine(ResetAllTiles());
    }

    // ── Solve ────────────────────────────────────────────────

    private IEnumerator SolvePuzzle()
    {
        isSolved = true;
        PlaySound(solvedSound);
        UpdateStatus(solvedMessage);

        yield return new WaitForSeconds(1f);

        if (clueObject != null || cluePaper != null)
        {
            ShowClueObject();
        }
        else
        {
            ShowCluePanel();
        }

        onPuzzleSolved?.Invoke();

        if (updateObjectiveGuide)
            ObjectiveManager.Instance?.SetObjective(solvedObjective);
    }

    // ── Reset ────────────────────────────────────────────────

    private IEnumerator ResetAllTiles()
    {
        UpdateStatus(wrongStepMessage);

        yield return new WaitForSeconds(wrongResetDelay);

        foreach (TileSwitchTile tile in tiles)
        {
            if (tile != null)
                tile.ResetVisual();
        }

        currentStep = 0;
        UpdateStatus(startMessage);
    }

    // ── Clue Panel ───────────────────────────────────────────

    private void ShowCluePanel()
    {
        if (cluePanel != null)
            cluePanel.SetActive(true);

        if (clueText != null)
            clueText.text = clueMessage;
    }

    private void ShowClueObject()
    {
        if (cluePaper != null)
        {
            cluePaper.documentTitle = clueTitle;
            cluePaper.documentText = clueMessage;
        }

        if (clueObject != null)
            clueObject.SetActive(true);

        UpdateStatus("A note has appeared. Press F to read it.");
    }

    private void HideCluePanel()
    {
        if (cluePanel != null)
            cluePanel.SetActive(false);
    }

    private void HideClueObject()
    {
        if (clueObject != null)
            clueObject.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────

    private TileSwitchTile GetTileByIndex(int index)
    {
        foreach (TileSwitchTile tile in tiles)
        {
            if (tile != null && tile.tileIndex == index)
                return tile;
        }

        return null;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // ── Gizmos ───────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (tiles == null || correctSequence == null)
            return;

        // Draw the correct sequence order as numbered lines in Scene view
        for (int i = 0; i < correctSequence.Length - 1; i++)
        {
            TileSwitchTile from = GetTileByIndex(correctSequence[i]);
            TileSwitchTile to   = GetTileByIndex(correctSequence[i + 1]);

            if (from == null || to == null)
                continue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(from.transform.position, to.transform.position);
            Gizmos.DrawSphere(from.transform.position + Vector3.up * 0.2f, 0.1f);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

public class StoryDialogueInteractable : InteractableBase
{
    [Header("Dialogue")]
    public string speaker = "";
    [TextArea(2, 4)] public string[] lockedLines;
    [TextArea(2, 4)] public string[] unlockedLines;

    [Header("State")]
    public bool startsUnlocked = false;
    public bool playOnlyOnce = false;
    public bool lockPlayerWhileTalking = false;

    [Header("Objective Guide")]
    public bool updateObjectiveAfterLockedDialogue = false;
    public string lockedObjectiveAfterDialogue = "";
    public bool updateObjectiveAfterUnlockedDialogue = false;
    public string unlockedObjectiveAfterDialogue = "";

    [Header("Events")]
    public UnityEvent onFirstUnlockedInteraction;

    private bool isUnlocked;
    private bool hasPlayedUnlocked;

    private void Awake()
    {
        isUnlocked = startsUnlocked;
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Inspect";
        interactionRadius = 2.5f;
    }

    public override string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (playOnlyOnce && isUnlocked && hasPlayedUnlocked)
            return;

        string[] lines = isUnlocked ? unlockedLines : lockedLines;

        UnityEvent onDialogueComplete = new UnityEvent();
        onDialogueComplete.AddListener(UpdateObjectiveAfterDialogue);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayDialogue(lines, speaker, lockPlayerWhileTalking, player, onDialogueComplete);
        }
        else if (lines != null && lines.Length > 0)
        {
            HintManager.Instance?.ShowHint(lines[0]);
            UpdateObjectiveAfterDialogue();
        }

        if (isUnlocked && !hasPlayedUnlocked)
        {
            hasPlayedUnlocked = true;
            onFirstUnlockedInteraction?.Invoke();
        }
    }

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void Lock()
    {
        isUnlocked = false;
    }

    private void UpdateObjectiveAfterDialogue()
    {
        if (!isUnlocked && updateObjectiveAfterLockedDialogue)
        {
            ObjectiveManager.Instance?.SetObjective(lockedObjectiveAfterDialogue);
            return;
        }

        if (isUnlocked && updateObjectiveAfterUnlockedDialogue)
            ObjectiveManager.Instance?.SetObjective(unlockedObjectiveAfterDialogue);
    }
}

using UnityEngine;
using UnityEngine.Events;

public class StoryDialogueEvent : MonoBehaviour
{
    [Header("Dialogue")]
    public string speaker = "";
    [TextArea(2, 4)] public string[] lines;
    public bool lockPlayerWhileTalking = false;

    [Header("Objective Guide")]
    public bool updateObjectiveAfterDialogue = false;
    public string objectiveAfterDialogue = "";

    [Header("Events")]
    public UnityEvent onDialogueFinished;

    public void Play()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        UnityEvent completeEvent = new UnityEvent();
        completeEvent.AddListener(UpdateObjectiveAfterDialogue);
        completeEvent.AddListener(onDialogueFinished.Invoke);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayDialogue(lines, speaker, lockPlayerWhileTalking, player, completeEvent);
        }
        else if (lines != null && lines.Length > 0)
        {
            HintManager.Instance?.ShowHint(lines[0]);
            UpdateObjectiveAfterDialogue();
            onDialogueFinished?.Invoke();
        }
    }

    private void UpdateObjectiveAfterDialogue()
    {
        if (updateObjectiveAfterDialogue)
            ObjectiveManager.Instance?.SetObjective(objectiveAfterDialogue);
    }
}

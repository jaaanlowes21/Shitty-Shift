using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StoryDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public string speaker = "";
    [TextArea(2, 4)] public string[] lines;
    public bool playOnlyOnce = true;
    public bool lockPlayerWhileTalking = false;

    [Header("Objective Guide")]
    public bool updateObjectiveAfterDialogue = false;
    public string objectiveAfterDialogue = "";

    [Header("Events")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueFinished;

    private bool hasPlayed;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnlyOnce && hasPlayed)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        hasPlayed = true;
        onDialogueStarted?.Invoke();

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

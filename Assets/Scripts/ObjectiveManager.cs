using TMPro;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("UI")]
    public GameObject objectivePanel;
    public TextMeshProUGUI objectiveText;

    [Header("Text")]
    public string prefix = "Objective: ";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    public void SetObjective(string objective)
    {
        if (objectiveText == null)
            return;

        objectiveText.text = $"{prefix}{objective}";

        if (objectivePanel != null)
            objectivePanel.SetActive(!string.IsNullOrWhiteSpace(objective));
    }

    public void ClearObjective()
    {
        if (objectiveText != null)
            objectiveText.text = string.Empty;

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }
}

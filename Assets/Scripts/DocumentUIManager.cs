using UnityEngine;
using TMPro;

public class DocumentUIManager : MonoBehaviour
{
    public static DocumentUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject documentPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    private ReadablePaper currentPaper;

    public bool IsOpen => documentPanel != null && documentPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (documentPanel != null)
            documentPanel.SetActive(false);
    }

    public void OpenDocument(ReadablePaper paper, PlayerMovement player)
    {
        if (paper == null) return;
        if (documentPanel == null || bodyText == null)
        {
            Debug.LogWarning("DocumentUIManager is missing UI references.");
            return;
        }

        currentPaper = paper;

        documentPanel.SetActive(true);

        if (titleText != null)
            titleText.text = paper.documentTitle;

        bodyText.text = paper.documentText;

        if (player != null)
            player.SetReadingState(true);
    }

    public void CloseDocument(PlayerMovement player)
    {
        if (documentPanel != null)
            documentPanel.SetActive(false);

        currentPaper = null;

        if (player != null)
            player.SetReadingState(false);
    }

    public bool IsShowing(ReadablePaper paper)
    {
        return IsOpen && currentPaper == paper;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtonActions : MonoBehaviour
{
    [Header("Play")]
    public string playSceneName = "GameScene";

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Fade Objects Inside Main Menu")]
    public CanvasGroup[] mainMenuFadeObjects;

    [Header("Fade Objects Inside Options")]
    public CanvasGroup[] optionsFadeObjects;

    [Header("Moving Object Before Options")]
    public Transform objectToMove;
    public float targetZ = -1.65f;
    public float moveDuration = 1f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private Vector3 originalPosition;
    private bool isBusy;

    private void Start()
    {
        if (objectToMove != null)
            originalPosition = objectToMove.position;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        SetGroups(mainMenuFadeObjects, 1f, true);
        SetGroups(optionsFadeObjects, 0f, false);
    }

    public void PlayGame()
    {
        if (isBusy) return;
        StartCoroutine(PlayRoutine());
    }

    public void OpenOptions()
    {
        if (isBusy) return;
        StartCoroutine(OpenOptionsRoutine());
    }

    public void CloseOptions()
    {
        if (isBusy) return;
        StartCoroutine(CloseOptionsRoutine());
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    private IEnumerator PlayRoutine()
    {
        isBusy = true;

        yield return FadeGroups(mainMenuFadeObjects, 1f, 0f, false);

        // Start the timer when beginning the game
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }
        else
        {
            Debug.Log("[MainMenu] GameTimer not found, it will be created in the next scene.");
        }

        SceneManager.LoadScene(playSceneName);
    }

    private IEnumerator OpenOptionsRoutine()
    {
        isBusy = true;

        yield return FadeGroups(mainMenuFadeObjects, 1f, 0f, false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (objectToMove != null)
        {
            Vector3 startPos = objectToMove.position;
            Vector3 endPos = new Vector3(startPos.x, startPos.y, targetZ);
            yield return MoveObject(objectToMove, startPos, endPos);
        }

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        yield return FadeGroups(optionsFadeObjects, 0f, 1f, true);

        isBusy = false;
    }

    private IEnumerator CloseOptionsRoutine()
    {
        isBusy = true;

        yield return FadeGroups(optionsFadeObjects, 1f, 0f, false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (objectToMove != null)
            yield return MoveObject(objectToMove, objectToMove.position, originalPosition);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        yield return FadeGroups(mainMenuFadeObjects, 0f, 1f, true);

        isBusy = false;
    }

    private IEnumerator MoveObject(Transform target, Vector3 from, Vector3 to)
    {
        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            target.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.position = to;
    }

    private IEnumerator FadeGroups(CanvasGroup[] groups, float from, float to, bool enableInteraction)
    {
        if (groups == null)
            yield break;

        foreach (CanvasGroup group in groups)
        {
            if (group == null) continue;

            group.gameObject.SetActive(true);
            group.alpha = from;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, timer / fadeDuration);

            foreach (CanvasGroup group in groups)
            {
                if (group != null)
                    group.alpha = alpha;
            }

            yield return null;
        }

        SetGroups(groups, to, enableInteraction);
    }

    private void SetGroups(CanvasGroup[] groups, float alpha, bool interactable)
    {
        if (groups == null)
            return;

        foreach (CanvasGroup group in groups)
        {
            if (group == null) continue;

            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }
}
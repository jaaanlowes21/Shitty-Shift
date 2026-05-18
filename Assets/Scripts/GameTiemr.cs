using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    private float elapsedTime = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        elapsedTime = 0f;
        Debug.Log("[GameTimer] Timer started!");
    }

    public void StopTimer()
    {
        isRunning = false;
        Debug.Log($"[GameTimer] Timer stopped! Total time: {GetFormattedTime()}");
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public string GetFormattedTime()
    {
        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (minutes > 0)
            return $"Time: {minutes} minute{(minutes > 1 ? "s" : "")} and {seconds} second{(seconds != 1 ? "s" : "")}";
        else
            return $"Time: {seconds} second{(seconds != 1 ? "s" : "")}";
    }

    public string GetShortFormattedTime()
    {
        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"Time: {minutes:D2}:{seconds:D2}";
    }

    private void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
        }
    }
}
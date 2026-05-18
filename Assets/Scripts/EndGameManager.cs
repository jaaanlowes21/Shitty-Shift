using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timeText;
    public Button menuButton;
    public Button quitButton;

    [Header("Time Display")]
    public string timePrefix = "Time: ";
    public string timeSuffix = " seconds";

    [Header("Typewriter Effect")]
    public float typewriterDelay = 0.05f; // Delay between each character
    public float revealDelay = 1f; // Delay before starting typewriter

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM")]
    public AudioClip endGameBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("UI Sounds")]
    public AudioClip buttonClickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    [Header("Time Reveal Sound")]
    public AudioClip timeRevealSound;
    [Range(0f, 1f)] public float timeRevealVolume = 0.8f;
    
    [Header("Typewriter Sounds")]
    public AudioClip typewriterTickSound; // Optional: tick sound for each character
    [Range(0f, 1f)] public float tickVolume = 0.3f;

    private Coroutine timeRevealCoroutine;

    private void Awake()
    {
        SetupAudioSources();
    }

    private void Start()
    {
        // Setup button listeners
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuPressed);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);

        // Hide time initially for reveal effect
        if (timeText != null)
            timeText.text = "";

        // Show cursor for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play BGM
        PlayBGM();

        // Start time reveal with typewriter
        timeRevealCoroutine = StartCoroutine(RevealTimeRoutine());
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void PlayBGM()
    {
        if (endGameBGM == null || bgmSource == null)
            return;

        bgmSource.clip = endGameBGM;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private IEnumerator RevealTimeRoutine()
    {
        // Wait before starting
        yield return new WaitForSeconds(revealDelay);

        // Get the time string
        string fullText = GetTimeString();

        if (string.IsNullOrEmpty(fullText))
            yield break;

        // Play reveal sound at start
        PlaySFX(timeRevealSound, timeRevealVolume);

        // Typewriter effect
        if (timeText != null)
        {
            timeText.text = "";

            foreach (char c in fullText)
            {
                timeText.text += c;

                // Play tick sound for each character (optional)
                if (typewriterTickSound != null && c != ' ')
                {
                    PlaySFX(typewriterTickSound, tickVolume);
                }

                yield return new WaitForSeconds(typewriterDelay);
            }
        }
    }

    private string GetTimeString()
    {
        if (GameTimer.Instance != null)
        {
            float time = GameTimer.Instance.GetElapsedTime();
            int totalSeconds = Mathf.FloorToInt(time);
            
            return $"{timePrefix}{totalSeconds}{timeSuffix}";
        }
        else
        {
            Debug.LogWarning("[EndGameManager] GameTimer Instance not found!");
            return "Time: N/A";
        }
    }

    public void OnMenuPressed()
    {
        PlaySFX(buttonClickSound, clickVolume);
        
        // Destroy the timer since we're going to main menu
        if (GameTimer.Instance != null)
            Destroy(GameTimer.Instance.gameObject);

        StartCoroutine(LoadSceneDelayed("MainMenu"));
    }

    public void OnQuitPressed()
    {
        PlaySFX(buttonClickSound, clickVolume);
        
        StartCoroutine(QuitDelayed());
    }

    private IEnumerator LoadSceneDelayed(string sceneName)
    {
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator QuitDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class InGameMenu : MonoBehaviour
{
    [Header("Menu Panel")]
    public GameObject menuPanel;
    
    [Header("Buttons")]
    public Button backButton;
    public Button menuButton;
    public Button quitButton;

    [Header("Sliders")]
    public Slider volumeSlider;
    public Slider sfxSlider;

    [Header("Audio Mixer (Optional)")]
    public AudioMixer audioMixer; // For more advanced control, optional

    private bool isPaused = false;
    private bool isPanelOpen = false;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        
        // Make sure panel starts closed
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void Start()
    {
        // Setup button listeners
        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuPressed);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);

        // Setup slider listeners
        if (volumeSlider != null)
        {
            // Load saved volume or set to default
            volumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (sfxSlider != null)
        {
            // Load saved SFX volume or set to default
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        // Set initial volumes
        OnVolumeChanged(volumeSlider != null ? volumeSlider.value : 0.5f);
        OnSFXChanged(sfxSlider != null ? sfxSlider.value : 1f);
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    private void Update()
    {
        // Check for P key press
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // Also close with ESC when panel is open
        if (isPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBackPressed();
        }
    }

    public void ToggleMenu()
    {
        if (isPanelOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        if (menuPanel == null) return;

        isPanelOpen = true;
        menuPanel.SetActive(true);
        
        Time.timeScale = 0f; // Pause the game
        
        // Show cursor for menu navigation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu()
    {
        if (menuPanel == null) return;

        isPanelOpen = false;
        menuPanel.SetActive(false);
        
        Time.timeScale = 1f; // Resume the game
        
        // Hide cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnBackPressed()
    {
        CloseMenu();
    }

    public void OnMenuPressed()
    {
        // Save settings before leaving
        SaveSettings();
        
        // Resume time before loading new scene
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene("MainMenu"); // Make sure your main menu scene is named "MainMenu"
    }

    public void OnQuitPressed()
    {
        // Save settings before quitting
        SaveSettings();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnVolumeChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("BGMVolume", value);
        PlayerPrefs.Save();

        // Apply to AudioManager (Floor 1)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        // Apply to AudioManager2 (Floor 2)
        if (AudioManager2.Instance != null)
        {
            AudioManager2.Instance.SetBGMVolume(value);
        }

        // Apply to MainMenuAudioManager
        if (MainMenuAudioManager.Instance != null)
        {
            MainMenuAudioManager.Instance.SetBGMVolume(value);
        }

        // If using AudioMixer
        if (audioMixer != null)
        {
            // Convert linear 0-1 to logarithmic dB (-80 to 0)
            float dB = value > 0.001f ? Mathf.Log10(value) * 20 : -80f;
            audioMixer.SetFloat("BGMVolume", dB);
        }
    }

    public void OnSFXChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        // Apply to MainMenuAudioManager
        if (MainMenuAudioManager.Instance != null)
        {
            MainMenuAudioManager.Instance.SetSFXVolume(value);
        }

        // For AudioManager and AudioManager2, you need to add SetSFXVolume method
        // Apply to all other audio sources through a master SFX volume
        ApplySFXVolumeToAll(value);

        // If using AudioMixer
        if (audioMixer != null)
        {
            float dB = value > 0.001f ? Mathf.Log10(value) * 20 : -80f;
            audioMixer.SetFloat("SFXVolume", dB);
        }
    }

    private void ApplySFXVolumeToAll(float volume)
    {
        // Find all AudioSources in the scene and adjust their volume
        // This is a simple approach - for better control, use an AudioMixer
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        
        foreach (AudioSource source in allAudioSources)
        {
            // Only adjust SFX sources (not BGM)
            // You can tag your audio sources or check their output
            if (source.outputAudioMixerGroup == null || !source.loop)
            {
                // Don't adjust, let each manager handle its own SFX
                // This is handled by individual audio managers
            }
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        SaveSettings();
    }
}
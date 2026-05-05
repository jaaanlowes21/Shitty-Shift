using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuAudioManager : MonoBehaviour
{
    public static MainMenuAudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource clickSource;
    public AudioSource ambientSource;

    [Header("BGM")]
    public AudioClip mainMenuBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("Mouse Click Sound")]
    public AudioClip mouseClickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    [Header("Repeating SFX")]
    public AudioClip repeatingSFX;
    public float repeatingSFXInterval = 5f;
    [Range(0f, 1f)] public float ambientVolume = 0.8f;

    [Header("Global SFX Volume")]
    [Range(0f, 1f)] public float sfxMasterVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupSources();
    }

    private void Start()
    {
        PlayBGM();
        StartCoroutine(RepeatingSFXRoutine());
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlayMouseClick();
        }
    }

    private void SetupSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        if (clickSource == null)
            clickSource = gameObject.AddComponent<AudioSource>();

        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        clickSource.loop = false;
        ambientSource.loop = false;
    }

    private void PlayBGM()
    {
        if (mainMenuBGM == null) return;

        bgmSource.clip = mainMenuBGM;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    private void PlayMouseClick()
    {
        if (mouseClickSound == null) return;

        clickSource.PlayOneShot(mouseClickSound, clickVolume * sfxMasterVolume);
    }

    private IEnumerator RepeatingSFXRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(repeatingSFXInterval);

            if (repeatingSFX != null)
                ambientSource.PlayOneShot(repeatingSFX, ambientVolume * sfxMasterVolume);
        }
    }

    // 🔥 for future sliders
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxMasterVolume = volume;
    }
}
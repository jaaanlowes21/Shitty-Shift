using System.Collections;
using UnityEngine;

public class AudioManager2 : MonoBehaviour
{
    public static AudioManager2 Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;

    [Header("BGM")]
    public AudioClip gameBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("Eerie Ambient")]
    public AudioClip[] eerieSounds;
    public float eerieInterval = 30f;
    [Range(0f, 1f)] public float eerieVolume = 0.7f;

    [Header("Poop Enemy Sounds")]
    public AudioClip teleportSound; // Plays before enemy sinks to room
    [Range(0f, 1f)] public float teleportVolume = 0.8f;
    
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackVolume = 1f;
    
    public AudioClip detectionSound; // Plays when enemy spots player
    [Range(0f, 1f)] public float detectionVolume = 0.9f;
    
    public AudioClip confusedSound; // Plays when enemy loses player
    [Range(0f, 1f)] public float confusedVolume = 0.7f;

    private Coroutine eerieRoutine;
    private float sfxMasterVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupSources();
    }

    private void Start()
    {
        PlayGameBGM();
        eerieRoutine = StartCoroutine(EerieSoundRoutine());
    }

    public void SetSFXVolume(float volume)
    {
        sfxMasterVolume = volume;
    }

    private void SetupSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        ambientSource.loop = false;
        ambientSource.playOnAwake = false;
    }

    public void PlayGameBGM()
    {
        if (gameBGM == null || bgmSource == null)
            return;

        bgmSource.clip = gameBGM;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // Poop Enemy Sounds
    public void PlayTeleportSound()
    {
        PlaySFX(teleportSound, teleportVolume);
    }

    public void PlayAttackSound()
    {
        PlaySFX(attackSound, attackVolume);
    }

    public void PlayDetectionSound()
    {
        PlaySFX(detectionSound, detectionVolume);
    }

    public void PlayConfusedSound()
    {
        PlaySFX(confusedSound, confusedVolume);
    }

    private IEnumerator EerieSoundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(eerieInterval);

            if (eerieSounds != null && eerieSounds.Length > 0)
            {
                AudioClip clip = eerieSounds[Random.Range(0, eerieSounds.Length)];
                PlayAmbient(clip, eerieVolume);
            }
        }
    }

    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume * sfxMasterVolume);
    }

    private void PlayAmbient(AudioClip clip, float volume)
    {
        if (clip == null || ambientSource == null)
            return;

        ambientSource.PlayOneShot(clip, volume * sfxMasterVolume);
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
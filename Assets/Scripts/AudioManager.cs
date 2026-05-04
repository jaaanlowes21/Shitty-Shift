using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class MonsterSound
    {
        public string monsterName;
        public AudioClip attackSound;
        [Range(0f, 1f)] public float attackVolume = 1f;
    }

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

    [Header("Lookout Sound")]
    public AudioClip lookoutSound;
    [Range(0f, 1f)] public float lookoutVolume = 1f;

    [Header("Boss Random Sound")]
    public AudioClip[] bossRandomSounds;
    public float bossRandomMinTime = 15f;
    public float bossRandomMaxTime = 25f;
    [Range(0f, 1f)] public float bossRandomVolume = 1f;

    [Header("Monster Attack Sounds")]
    public MonsterSound[] monsterAttackSounds;

    private Coroutine eerieRoutine;
    private Coroutine bossRandomRoutine;

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
        bossRandomRoutine = StartCoroutine(BossRandomSoundRoutine());
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

    public void PlayLookoutSound()
    {
        PlaySFX(lookoutSound, lookoutVolume);
    }

    public void PlayMonsterAttackSound(string monsterName)
    {
        if (monsterAttackSounds == null)
            return;

        foreach (MonsterSound monster in monsterAttackSounds)
        {
            if (monster.monsterName == monsterName)
            {
                PlaySFX(monster.attackSound, monster.attackVolume);
                return;
            }
        }

        Debug.LogWarning("No attack sound found for monster: " + monsterName);
    }

    public void PlayBossRandomSound()
    {
        if (bossRandomSounds == null || bossRandomSounds.Length == 0)
            return;

        AudioClip clip = bossRandomSounds[Random.Range(0, bossRandomSounds.Length)];
        PlaySFX(clip, bossRandomVolume);
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

    private IEnumerator BossRandomSoundRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(bossRandomMinTime, bossRandomMaxTime);
            yield return new WaitForSeconds(waitTime);

            PlayBossRandomSound();
        }
    }

    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayAmbient(AudioClip clip, float volume)
    {
        if (clip == null || ambientSource == null)
            return;

        ambientSource.PlayOneShot(clip, volume);
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
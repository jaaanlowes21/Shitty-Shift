using UnityEngine;
using UnityEngine.UI;

public class MainMenuOptionsVolume : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider bgmSlider;

    private const string MasterKey = "MasterVolume";
    private const string SFXKey = "SFXVolume";
    private const string BGMKey = "BGMVolume";

    private void Start()
    {
        float master = PlayerPrefs.GetFloat(MasterKey, 1f);
        float sfx = PlayerPrefs.GetFloat(SFXKey, 1f);
        float bgm = PlayerPrefs.GetFloat(BGMKey, 0.5f);

        if (masterSlider != null)
        {
            masterSlider.value = master;
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfx;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = bgm;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        ApplyAllVolumes(master, sfx, bgm);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        if (MainMenuAudioManager.Instance != null)
            MainMenuAudioManager.Instance.SetSFXVolume(value);

        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float value)
    {
        if (MainMenuAudioManager.Instance != null)
            MainMenuAudioManager.Instance.SetBGMVolume(value);

        PlayerPrefs.SetFloat(BGMKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyAllVolumes(float master, float sfx, float bgm)
    {
        AudioListener.volume = master;

        if (MainMenuAudioManager.Instance != null)
        {
            MainMenuAudioManager.Instance.SetSFXVolume(sfx);
            MainMenuAudioManager.Instance.SetBGMVolume(bgm);
        }
    }
}
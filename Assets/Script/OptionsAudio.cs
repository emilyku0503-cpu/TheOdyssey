using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsAudio : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicKey = "Settings.MusicVolume";
    private const string SfxKey = "Settings.SFXVolume";

    private void Start()
    {
        if (audioMixer == null || musicSlider == null || sfxSlider == null)
        {
            Debug.LogError(
                "OptionsAudio: assign the mixer and both sliders.", this);
            enabled = false;
            return;
        }

        ConfigureSlider(musicSlider);
        ConfigureSlider(sfxSlider);

        float music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));

        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);

        ApplyVolume("MusicVolume", music);
        ApplyVolume("SFXVolume", sfx);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void ConfigureSlider(Slider slider)
    {
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private void SetMusicVolume(float value)
    {
        ApplyVolume("MusicVolume", value);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    private void SetSfxVolume(float value)
    {
        ApplyVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SfxKey, value);
    }

    private void ApplyVolume(string parameter, float value)
    {
        // Convert the slider's 0–1 range to mixer decibels.
        // Zero uses the mixer's minimum level: effectively silent.
        float decibels = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        if (!audioMixer.SetFloat(parameter, decibels))
        {
            Debug.LogWarning(
                $"Expose a mixer volume parameter named '{parameter}'.",
                this);
        }
    }

    public void SaveVolumes()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveVolumes();
    }

    private void OnApplicationQuit()
    {
        SaveVolumes();
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }
}
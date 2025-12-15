using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    public GameObject optionsMenu;
    public GameObject pauseMenu;

    public static bool cursorHiddenSetting = false;

    void Start()
    {
        var settings = PersistentSettings.Instance;

        musicSlider.SetValueWithoutNotify(settings.musicVolume);
        sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("sfxVolume", volume);

        PersistentSettings.Instance.sfxVolume = volume;
        PersistentSettings.Instance.Save();
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("musicVolume", volume);

        PersistentSettings.Instance.musicVolume = volume;
        PersistentSettings.Instance.Save();
    }

    public void BackToPause()
    {
        // If we press BACK, then go back to Pause Menu panel
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }
}

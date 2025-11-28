using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;   // Main Mixer to adjust, allowing either SFX or BGM to change
    public GameObject optionsMenu;  // Options menu for being able to deactivate when going back to previous panel, being pause
    public GameObject pauseMenu;    // Pause menu to reactivate later

    public static bool cursorHiddenSetting = false; // Static variable for cursor being hidden or visible

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("sfxVolume", volume);   // If we are adjusting SFX slider object, then change the SFX portion of the mixer
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("musicVolume", volume); // If we are adjusting BGM slider object, then change the BGM portion of the mixer
    }

    public void BackToPause()
    {
        // If we press BACK, then go back to Pause Menu panel
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }
}

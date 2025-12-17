using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PersistentSettings : MonoBehaviour
{
    public static PersistentSettings Instance;

    [Range(-80f, 0f)] public float musicVolume = 0f;
    [Range(-80f, 0f)] public float sfxVolume = 0f;

    public AudioMixer audioMixer; // ASSIGN THIS IN INSPECTOR

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // load saved values
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0f);

        // APPLY THEM IMMEDIATELY
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        audioMixer.SetFloat("musicVolume", musicVolume);
        audioMixer.SetFloat("sfxVolume", sfxVolume);
        Debug.Log($"ApplyVolumes | music={musicVolume}, sfx={sfxVolume}");

    }

    public void Save()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVolumes();
    }
}

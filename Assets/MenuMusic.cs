using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MenuMusic : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioMixer audioMixer;

    public float fadeOutDuration = 2f;

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (!musicSource.isPlaying)
            musicSource.Play();

        PersistentSettings.Instance.ApplyVolumes();
    }


    public IEnumerator FadeOut()
    {
        float t = 0f;

        // Start from current saved volume
        float startVolume = PersistentSettings.Instance.musicVolume;

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime; // Important if Time.timeScale = 0
            float v = Mathf.Lerp(startVolume, -80f, t / fadeOutDuration);
            audioMixer.SetFloat("musicVolume", v);
            yield return null;
        }

        audioMixer.SetFloat("musicVolume", -80f);
        musicSource.Stop();
    }
}

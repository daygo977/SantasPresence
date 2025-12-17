using System.Collections;
using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    public AudioSource musicSource;             // Music source
    public float fadeDuration = 4f;             // Amount of time to fade to 0
    public float targetVol;                     // The target volume, (sometimes it's best if we don't set this to 1 for all music)

    // Ensure musicSource is assigned so we can start fading in the music with FadeIn()
    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        StartCoroutine(FadeIn());
    }

    // Starting from volume: 0, we interpolate the volume throughout a duration until we reach the target volume
    public IEnumerator FadeIn()
    {
        float startVol = 0f;

        float t = 0f;

        // make sure audio actually plays
        if (!musicSource.isPlaying)
            musicSource.Play();

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, targetVol, t / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVol;
    }

    // Similar to function above, but fading out the music rather than fading in
    public IEnumerator FadeOut()
    {
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }
}

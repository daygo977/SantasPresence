using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpeedBoostUI : MonoBehaviour
{
    [Header("UI")]
    public Image boostIcon;

    [Header("Blink Settings")]
    public float slowBlinkRate = 1.2f; // seconds per blink (early)
    public float fastBlinkRate = 0.15f; // seconds per blink (near end)
    public float blinkStartPercent = 0.4f; // start blinking at 40% remaining

    private Coroutine boostRoutine;

    void Awake()
    {
        SetAlpha(0f);
    }

    void OnEnable()
    {
        FirstPersonPlayer.OnSpeedBoostStarted += OnBoostStarted;
        FirstPersonPlayer.OnSpeedBoostEnded += OnBoostEnded;
    }

    void OnDisable()
    {
        FirstPersonPlayer.OnSpeedBoostStarted -= OnBoostStarted;
        FirstPersonPlayer.OnSpeedBoostEnded -= OnBoostEnded;
    }

    void OnBoostStarted(float duration)
    {
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);

        boostRoutine = StartCoroutine(BoostVisualRoutine(duration));
    }

    void OnBoostEnded()
    {
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);

        SetAlpha(0f);
    }

    IEnumerator BoostVisualRoutine(float duration)
    {
        float elapsed = 0f;
        float blinkTimer = 0f;
        bool visible = true;

        // Immediately solid
        SetAlpha(1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float remainingPercent = 1f - (elapsed / duration);

            // No blinking
            if (remainingPercent > blinkStartPercent)
            {
                SetAlpha(1f);
                blinkTimer = 0f; // Reset so blink starts smoothly
            }
            // Blinking
            else
            {
                blinkTimer += Time.deltaTime;

                float blinkT = 1f - (remainingPercent / blinkStartPercent);
                float currentBlinkRate = Mathf.Lerp(
                    slowBlinkRate,
                    fastBlinkRate,
                    blinkT
                );

                if (blinkTimer >= currentBlinkRate)
                {
                    blinkTimer = 0f;
                    visible = !visible;
                    SetAlpha(visible ? 1f : 0f);
                }
            }

            yield return null;
        }

        SetAlpha(0f);
    }


    void SetAlpha(float a)
    {
        if (!boostIcon) return;

        Color c = boostIcon.color;
        c.a = a;
        boostIcon.color = c;
    }
}

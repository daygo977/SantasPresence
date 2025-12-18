using UnityEngine;
using UnityEngine.UI;

public class HoldToEatCookies : MonoBehaviour
{
    [Header("Interaction")]
    public float holdTime = 3f;
    public GameObject uiObject;
    public Slider progressSlider;

    private bool playerInside = false;
    private float holdProgress = 0f;
    private bool canEat = true;

    [Header("Speed Boost")]
    public float speedMultiplier = 1.25f;
    public float boostDuration = 8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip eatSound;

    [Range(0f, 1f)]
    public float eatVolume = 1f;

    [Header("Noise")]
    public float eatNoiseRadius = 4f;
    public LayerMask enemyLayer;

    private FirstPersonPlayer player;

    void Start()
    {
        uiObject.SetActive(false);
        progressSlider.value = 0f;
    }

    void Update()
    {
        if (!playerInside || !canEat)
            return;

        if (Input.GetKey(KeyCode.F))
        {
            holdProgress += Time.deltaTime;
            progressSlider.value = holdProgress / holdTime;

            if (holdProgress >= holdTime)
            {
                CompleteInteraction();
            }
        }
        else
        {
            holdProgress -= Time.deltaTime * 2f;
            holdProgress = Mathf.Max(0f, holdProgress);
            progressSlider.value = holdProgress / holdTime;
        }
    }

    private void CompleteInteraction()
    {
        canEat = false;
        uiObject.SetActive(false);
        playerInside = false;

        if (player != null)
        {
            player.ApplySpeedBoost(speedMultiplier, boostDuration);
        }

        PlayEatSound();
        EmitEatNoise();

        gameObject.SetActive(false);
    }

    void PlayEatSound()
    {
        if (audioSource && eatSound)
        {
            audioSource.PlayOneShot(eatSound, eatVolume);
        }
    }

    void EmitEatNoise()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            eatNoiseRadius,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            EnemyHearing enemy = hit.GetComponentInParent<EnemyHearing>();
            if (enemy != null)
            {
                enemy.HearNoise(transform.position, eatNoiseRadius);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canEat)
        {
            playerInside = true;
            uiObject.SetActive(true);
            player = other.GetComponent<FirstPersonPlayer>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && canEat)
        {
            playerInside = false;
            holdProgress = 0f;
            progressSlider.value = 0f;
            uiObject.SetActive(false);
        }
    }
}

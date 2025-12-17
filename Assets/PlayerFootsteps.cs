using UnityEngine;
using UnityEngine.Audio;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    public FirstPersonPlayer player;
    public AudioSource source;
    public AudioClip footstepClip;

    [Header("Step Timing")]
    public float walkInterval = 0.5f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.7f;

    [Header("Audio")]
    public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

    private double nextStepTime;
    private bool wasMovingLastFrame;

    [Header("Anti-Spam")]
    public float minStepCooldown = 0.2f;
    public float minDistancePerStep = 0.88f;

    private double lastStepTime;
    private Vector3 lastStepPosition;

    [Header("Hearing")]
    public float walkNoiseRadius = 5f;
    public float runNoiseRadius = 10f;
    public float crouchNoiseRadius = 2f;
    public LayerMask enemyLayer;

    void Start()
    {
        source.playOnAwake = false;
        source.loop = false;

        double dsp = AudioSettings.dspTime;
        nextStepTime = dsp;
        lastStepTime = dsp;
        lastStepPosition = transform.position;
    }

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        // not moving = no footsteps
        if (!player || !player.IsMoving)
        {
            wasMovingLastFrame = false;
            return;
        }

        float interval;

        if (player.IsCrouching)
            interval = crouchInterval;
        else if (player.IsRunning)
            interval = runInterval;
        else
            interval = walkInterval;

        double dspTime = AudioSettings.dspTime;

        // clean reset when movement starts
        if (!wasMovingLastFrame)
        {
            nextStepTime = dspTime;
        }

        if (dspTime >= nextStepTime)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastStepPosition);
            double timeSinceLastStep = dspTime - lastStepTime;

            if (distanceMoved >= minDistancePerStep &&
                timeSinceLastStep >= minStepCooldown)
            {
                PlayScheduledStep();

                lastStepTime = dspTime;
                lastStepPosition = transform.position;
                nextStepTime = dspTime + interval;
            }
        }

        wasMovingLastFrame = true;
    }

    void PlayScheduledStep()
    {
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.clip = footstepClip;
        source.PlayScheduled(AudioSettings.dspTime);

        EmitNoise();
    }

    void EmitNoise()
    {
        float radius;

        if (player.IsCrouching)
            radius = crouchNoiseRadius;
        else if (player.IsRunning)
            radius = runNoiseRadius;
        else
            radius = walkNoiseRadius;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            radius,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            EnemyHearing enemy = hit.GetComponentInParent<EnemyHearing>();
            if (enemy != null)
            {
                enemy.HearNoise(transform.position, radius);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.Audio;

public class PlayerFootsteps : MonoBehaviour
{
    public FPPlayerController player;
    public AudioSource source;
    public AudioClip footstepClip;

    public float walkInterval = 0.5f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.7f;

    public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

    private double nextStepTime;
    private bool wasMovingLastFrame;

    [Header("Anti-Spam")]
    public float minStepCooldown = 0.2f;      // absolute minimum time between steps
    public float minDistancePerStep = 0.88f;  // minimum movement distance

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

        nextStepTime = AudioSettings.dspTime;
        lastStepTime = AudioSettings.dspTime;
        lastStepPosition = transform.position;
    }

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        float interval;
        bool isMoving = true;

        switch (player.currentState)
        {
            case FPPlayerController.MoveState.Walk:
                interval = walkInterval;
                break;

            case FPPlayerController.MoveState.Run:
                interval = runInterval;
                break;

            case FPPlayerController.MoveState.Crouch:
                interval = crouchInterval;
                break;

            default:
                wasMovingLastFrame = false;
                return;
        }

        double dspTime = AudioSettings.dspTime;

        // reset timing cleanly when starting movement
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
        float radius = 0f;

        switch (player.currentState)
        {
            case FPPlayerController.MoveState.Walk:
                radius = walkNoiseRadius;
                break;

            case FPPlayerController.MoveState.Run:
                radius = runNoiseRadius;
                break;

            case FPPlayerController.MoveState.Crouch:
                radius = crouchNoiseRadius;
                break;
        }

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

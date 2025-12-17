using System.Collections;
using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class FPPlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minLookY = -80f;
    public float maxLookY = 80f;

    [Header("Crouch")]
    public float crouchHeight = 1.0f;
    public float crouchTransitionSpeed = 8f;

    [Header("Lean / Peek")]
    public float leanAngle = 15f;       // roll angle in degrees
    public float leanOffset = 0.3f;     // sideways camera offset
    public float leanSpeed = 10f;

    [Header("Noise Levels")]
    public float idleNoise = 0f;
    public float walkNoise = 0.3f;      // low noise
    public float runNoise = 1f;         // high noise
    public float crouchNoise = 0.05f;   // almost no noise

    [Header("Speed Buff")]
    public float speedMultiplier = 1f;

    [HideInInspector] public float currentNoiseLevel;
    public enum MoveState { Idle, Walk, Run, Crouch }
    [HideInInspector] public MoveState currentState;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 velocity;

    private bool isCrouching;
    private float standingHeight;
    private Vector3 defaultCameraLocalPos;

    private float currentLeanAngle = 0f;
    private float targetLeanAngle = 0f;
    private float targetLeanOffsetX = 0f;

    public static event Action<float> OnSpeedBoostStarted; // speed boost duration
    public static event Action OnSpeedBoostEnded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        standingHeight = controller.height;
        if (cameraTransform != null)
            defaultCameraLocalPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        HandleLean();
        ApplyGravity();
        UpdateNoiseLevel();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookY, maxLookY);

        // camera pitch + roll (for leaning)
        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(xRotation, 0f, currentLeanAngle);
        }

        // body yaw
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");  // WASD movement
        float z = Input.GetAxisRaw("Vertical");    

        Vector3 move = (transform.right * x + transform.forward * z);
        move = move.normalized;

        bool wantsToCrouch = Input.GetKey(KeyCode.LeftControl);
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && !wantsToCrouch && z > 0f;

        isCrouching = wantsToCrouch;

        float speed = walkSpeed;
        if (isCrouching)
            speed = crouchSpeed;
        else if (wantsToRun && move.magnitude > 0.1f)
            speed = runSpeed;

        speed *= speedMultiplier;

        controller.Move(move * speed * Time.deltaTime);

        // set move state
        if (move.magnitude < 0.1f)
        {
            currentState = isCrouching ? MoveState.Crouch : MoveState.Idle;
        }
        else
        {
            if (isCrouching) currentState = MoveState.Crouch;
            else if (speed == runSpeed) currentState = MoveState.Run;
            else currentState = MoveState.Walk;
        }
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        controller.height = Mathf.Lerp(controller.height, targetHeight,
            crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = controller.center;
        center.y = controller.height / 2f;
        controller.center = center;

        if (cameraTransform != null)
        {
            float heightRatio = controller.height / standingHeight;
            Vector3 camPos = cameraTransform.localPosition;
            float targetY = defaultCameraLocalPos.y * heightRatio;

            camPos.y = Mathf.Lerp(camPos.y, targetY,
                crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = camPos;
        }
    }

    private void HandleLean()
    {
        float leanInput = 0f;
        if (Input.GetKey(KeyCode.Q)) leanInput = -1f;   // lean left
        else if (Input.GetKey(KeyCode.E)) leanInput = 1f; // lean right

        targetLeanAngle = leanInput * leanAngle;
        targetLeanOffsetX = leanInput * leanOffset;

        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle,
            leanSpeed * Time.deltaTime);

        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.x = Mathf.Lerp(camPos.x,
                defaultCameraLocalPos.x + targetLeanOffsetX,
                leanSpeed * Time.deltaTime);
            cameraTransform.localPosition = camPos;
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateNoiseLevel()
    {
        switch (currentState)
        {
            case MoveState.Idle:
                currentNoiseLevel = idleNoise;
                break;
            case MoveState.Walk:
                currentNoiseLevel = walkNoise;
                break;
            case MoveState.Run:
                currentNoiseLevel = runNoise;
                break;
            case MoveState.Crouch:
                currentNoiseLevel = crouchNoise;
                break;
        }

        // Note to self - AI can read currentNoiseLevel from this component


    }
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        OnSpeedBoostStarted?.Invoke(duration);

        yield return new WaitForSeconds(duration);

        speedMultiplier = 1f;
        OnSpeedBoostEnded?.Invoke();
    }
}

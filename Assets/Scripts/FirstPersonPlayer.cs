using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;
    public bool allowAirControl = false;

    [Header("Speed Buff")]
    public float speedMultiplier = 1f;

    [Header("Keys")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode peekLeftKey = KeyCode.Q;
    public KeyCode peekRightKey = KeyCode.E;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookUp = 80f;
    public float maxLookDown = -80f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1.1f;
    public float crouchTransitionSpeed = 8f;

    [Header("Peeking")]
    public float peekAmount = 0.3f;
    public float peekSpeed = 8f;

    [Header("Noise")]
    public float walkNoise = 0.4f;
    public float runNoise = 1.0f;
    public float crouchNoise = 0.05f;
    public float noiseSmoothSpeed = 10f;

    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }
    public float CurrentNoise { get; private set; }

    public static event Action<float> OnSpeedBoostStarted;
    public static event Action OnSpeedBoostEnded;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    private float crouchLerp;
    private Vector3 standingCamLocalPos;
    private Vector3 crouchCamLocalPos;

    private float peekValue;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (!cameraTransform)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
            else Debug.LogError("No camera found.");
        }

        standingHeight = controller.height;
        standingCamLocalPos = cameraTransform.localPosition;

        float camYOffset = -(standingHeight - crouchHeight);
        crouchCamLocalPos = standingCamLocalPos + Vector3.up * camYOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandlePeek();
        HandleLook();
        HandleMovementAndStates();
        HandleCrouch();
        UpdateNoise();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, maxLookDown, maxLookUp);

        float roll = -peekValue * 10f;
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, roll);
    }

    private void HandleMovementAndStates()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        IsMoving = inputDir.sqrMagnitude > 0.001f;

        IsCrouching = Input.GetKey(crouchKey);
        IsRunning = Input.GetKey(runKey) && !IsCrouching && inputZ > 0.1f;

        float baseSpeed = walkSpeed;
        if (IsCrouching) baseSpeed = crouchSpeed;
        else if (IsRunning) baseSpeed = runSpeed;

        float finalSpeed = baseSpeed * speedMultiplier;

        Vector3 move = Vector3.zero;
        if (controller.isGrounded || allowAirControl)
        {
            move = (transform.right * inputX + transform.forward * inputZ).normalized;
            move *= finalSpeed;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        float target = IsCrouching ? 1f : 0f;
        crouchLerp = Mathf.MoveTowards(crouchLerp, target, crouchTransitionSpeed * Time.deltaTime);

        controller.height = Mathf.Lerp(standingHeight, crouchHeight, crouchLerp);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        Vector3 basePos = Vector3.Lerp(standingCamLocalPos, crouchCamLocalPos, crouchLerp);
        Vector3 peekOffset = Vector3.right * (peekAmount * peekValue);

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            basePos + peekOffset,
            crouchTransitionSpeed * Time.deltaTime
        );
    }

    private void HandlePeek()
    {
        float targetPeek = 0f;
        if (Input.GetKey(peekLeftKey)) targetPeek = -1f;
        else if (Input.GetKey(peekRightKey)) targetPeek = 1f;

        peekValue = Mathf.MoveTowards(peekValue, targetPeek, peekSpeed * Time.deltaTime);
    }

    private void UpdateNoise()
    {
        float targetNoise = 0f;

        if (IsMoving)
        {
            if (IsCrouching) targetNoise = crouchNoise;
            else if (IsRunning) targetNoise = runNoise;
            else targetNoise = walkNoise;
        }

        CurrentNoise = Mathf.Lerp(CurrentNoise, targetNoise, noiseSmoothSpeed * Time.deltaTime);
    }

    // Used for when cookies are consumed, modifying the speed multiplier and the duration of the speed boost
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

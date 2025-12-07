using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = -9.81f;
    public bool allowAirControl = false;

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
    public float peekAmount = 0.3f;     // how far to lean left/right
    public float peekSpeed = 8f;        // how fast we lean

    [Header("Noise")]
    public float walkNoise = 0.4f;
    public float runNoise = 1.0f;
    public float crouchNoise = 0.0f;    // crouch is silent
    public float noiseSmoothSpeed = 10f;

    // public read-only flags for other systems (AI, UI, etc.)
    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }
    public float CurrentNoise { get; private set; }

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    private float crouchLerp;               // 0 standing, 1 crouching
    private Vector3 standingCamLocalPos;
    private Vector3 crouchCamLocalPos;

    private float peekValue;                // -1 left, 0 center, 1 right

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError("FirstPersonPlayer: No Camera assigned or found as child.");
            }
        }

        // sync standing height with controller at start
        standingHeight = controller.height;

        standingCamLocalPos = cameraTransform.localPosition;

        // simple guess for crouch camera position (lowered)
        float camYOffset = -(standingHeight - crouchHeight);
        crouchCamLocalPos = standingCamLocalPos + new Vector3(0f, camYOffset, 0f);
    }

    private void Update()
    {
        HandlePeek();     // lean first so look uses correct roll
        HandleLook();
        HandleMovementAndStates();
        HandleCrouch();
        UpdateNoise();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // yaw
        transform.Rotate(Vector3.up * mouseX);

        // pitch
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, maxLookDown, maxLookUp);

        // roll for peeking
        float roll = -peekValue * 10f;   // small tilt when leaning

        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, roll);
    }

    private void HandleMovementAndStates()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        IsMoving = inputDir.sqrMagnitude > 0.001f;

        bool wantsCrouch = Input.GetKey(crouchKey);
        bool wantsRun = Input.GetKey(runKey) && !wantsCrouch && inputZ > 0.1f;

        IsRunning = wantsRun;
        IsCrouching = wantsCrouch;

        float currentSpeed = walkSpeed;
        if (IsCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (IsRunning)
        {
            currentSpeed = runSpeed;
        }

        Vector3 move = Vector3.zero;

        if (controller.isGrounded || allowAirControl)
        {
            move = transform.right * inputX + transform.forward * inputZ;
            move = move.normalized * currentSpeed;
        }

        // simple gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // apply
        Vector3 velocity = move + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        float target = IsCrouching ? 1f : 0f;
        crouchLerp = Mathf.MoveTowards(crouchLerp, target, crouchTransitionSpeed * Time.deltaTime);

        // smoothly resize character controller
        controller.height = Mathf.Lerp(standingHeight, crouchHeight, crouchLerp);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        // smoothly move camera height and apply peek offset
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
        if (Input.GetKey(peekLeftKey))
        {
            targetPeek = -1f;
        }
        else if (Input.GetKey(peekRightKey))
        {
            targetPeek = 1f;
        }

        peekValue = Mathf.MoveTowards(peekValue, targetPeek, peekSpeed * Time.deltaTime);
    }

    private void UpdateNoise()
    {
        float targetNoise = 0f;

        if (IsMoving)
        {
            if (IsCrouching)
            {
                targetNoise = crouchNoise;   // crouch movement is silent
            }
            else if (IsRunning)
            {
                targetNoise = runNoise;
            }
            else
            {
                targetNoise = walkNoise;
            }
        }

        CurrentNoise = Mathf.Lerp(CurrentNoise, targetNoise, noiseSmoothSpeed * Time.deltaTime);
    }
}

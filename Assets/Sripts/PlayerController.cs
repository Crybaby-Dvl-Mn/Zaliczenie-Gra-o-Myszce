using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Prêdkoœci – CHÓD (domyœlne)")]
    public float walkForward = 3f;
    public float walkBackward = 1.8f;
    public float walkStrafe = 2.2f;

    [Header("Prêdkoœci – BIEG (Shift)")]
    public float sprintForward = 6f;
    public float sprintBackward = 3.5f;
    public float sprintStrafe = 4.5f;

    [Header("Prêdkoœci – KUCANIE (Ctrl)")]
    public float crouchForward = 1.8f;
    public float crouchBackward = 1.2f;
    public float crouchStrafe = 1.5f;

    [Header("P³ynnoœæ ruchu")]
    public float acceleration = 8f;
    public float rotationSpeed = 10f;

    [Header("Start sprintu")]
    public float sprintSpinUpTime = 0.5f;

    [Header("Wysokoœci CharacterControllera")]
    [Tooltip("Wysokoœæ podczas biegu (nadpisuje domyœln¹ z CharacterControllera)")]
    public float sprintHeight = 1.7f;
    [Tooltip("Wysokoœæ podczas kucania (nadpisuje domyœln¹ z CharacterControllera)")]
    public float crouchHeight = 1.2f;
    [Tooltip("Jak szybko collider zmienia wysokoœæ")]
    public float heightChangeSpeed = 10f;

    [Header("Referencje")]
    public Transform cameraTransform;
    public CameraRig cameraRig;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private bool isSprinting;
    private bool isCrouching;
    private float sprintSpinUpTimer;
    private float targetHeight;

    // Domyœlna wysokoœæ – brana z CharacterControllera w Awake
    private float standHeight;
    private Vector3 standCenter;

    public bool IsSprintActive => isSprinting;
    public bool IsCrouching => isCrouching;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraRig == null && cameraTransform != null)
            cameraRig = cameraTransform.GetComponent<CameraRig>();

        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<CameraRig>();

        // Zapamiêtaj domyœln¹ wysokoœæ i center z CharacterControllera
        standHeight = controller.height;
        standCenter = controller.center;
        targetHeight = standHeight;
    }

    void Update()
    {
        HandleMovement();
        HandleSprintState();
        HandleCrouchState();
        HandleHeight();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float yaw = cameraRig != null ? cameraRig.CurrentYaw : 0f;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

        float sprintProgress = 0f;
        if (isSprinting)
        {
            if (sprintSpinUpTimer > 0f)
                sprintProgress = 1f - (sprintSpinUpTimer / sprintSpinUpTime);
            else
                sprintProgress = 1f;
        }

        float forwardSpeed, backwardSpeed, strafeSpeed;

        if (isCrouching)
        {
            forwardSpeed = crouchForward;
            backwardSpeed = crouchBackward;
            strafeSpeed = crouchStrafe;
        }
        else if (isSprinting)
        {
            forwardSpeed = Mathf.Lerp(walkForward, sprintForward, sprintProgress);
            backwardSpeed = Mathf.Lerp(walkBackward, sprintBackward, sprintProgress);
            strafeSpeed = Mathf.Lerp(walkStrafe, sprintStrafe, sprintProgress);
        }
        else
        {
            forwardSpeed = walkForward;
            backwardSpeed = walkBackward;
            strafeSpeed = walkStrafe;
        }

        Vector3 moveDir = Vector3.zero;

        if (Mathf.Abs(v) > 0.01f)
        {
            float speedForV = v > 0f ? forwardSpeed : backwardSpeed;
            moveDir += forward * v * speedForV;
        }

        if (Mathf.Abs(h) > 0.01f)
        {
            moveDir += right * h * strafeSpeed;
        }

        Vector3 targetVel = moveDir.magnitude > 0.01f
            ? Vector3.ClampMagnitude(moveDir, Mathf.Max(forwardSpeed, strafeSpeed, backwardSpeed))
            : Vector3.zero;

        Vector3 horizontalVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        horizontalVel = Vector3.Lerp(horizontalVel, targetVel, acceleration * Time.deltaTime);
        currentVelocity.x = horizontalVel.x;
        currentVelocity.z = horizontalVel.z;

        Vector3 lookDir = Vector3.zero;
        if (Mathf.Abs(h) > 0.01f || v > 0.01f)
        {
            lookDir = forward * Mathf.Max(v, 0f) + right * h;
        }

        if (lookDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (!controller.isGrounded)
            currentVelocity.y -= 9.81f;
        else
            currentVelocity.y = -1f;

        controller.Move(currentVelocity * Time.deltaTime);
    }

    void HandleSprintState()
    {
        if (isCrouching)
        {
            isSprinting = false;
            sprintSpinUpTimer = 0f;
            return;
        }

        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);
        bool hasMoveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f
                         || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;

        bool wantSprint = sprintHeld && hasMoveInput;

        if (wantSprint && !isSprinting)
        {
            isSprinting = true;
            sprintSpinUpTimer = sprintSpinUpTime;
        }
        else if (!wantSprint && isSprinting)
        {
            isSprinting = false;
            sprintSpinUpTimer = 0f;
        }

        if (sprintSpinUpTimer > 0f)
            sprintSpinUpTimer -= Time.deltaTime;
    }

    void HandleCrouchState()
    {
        bool crouchHeld = Input.GetKey(KeyCode.LeftControl);

        if (crouchHeld && !isCrouching)
        {
            isCrouching = true;
            isSprinting = false;
            sprintSpinUpTimer = 0f;
        }
        else if (!crouchHeld && isCrouching)
        {
            isCrouching = false;
        }
    }

    void HandleHeight()
    {
        // Docelowa wysokoœæ: kucanie > bieg > domyœlna (z CharacterControllera)
        if (isCrouching)
            targetHeight = crouchHeight;
        else if (isSprinting)
            targetHeight = sprintHeight;
        else
            targetHeight = standHeight;

        float newHeight = Mathf.Lerp(controller.height, targetHeight, heightChangeSpeed * Time.deltaTime);
        controller.height = newHeight;

        // Center: przy domyœlnej wysokoœci wracamy do standCenter, przy innych – height/2
        if (Mathf.Approximately(targetHeight, standHeight))
        {
            controller.center = Vector3.Lerp(controller.center, standCenter, heightChangeSpeed * Time.deltaTime);
        }
        else
        {
            controller.center = new Vector3(0f, newHeight * 0.5f, 0f);
        }
    }
}
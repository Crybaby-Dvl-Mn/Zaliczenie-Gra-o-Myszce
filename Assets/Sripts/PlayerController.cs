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
    public float sprintHeight = 1.7f;
    public float crouchHeight = 1.2f;
    public float heightChangeSpeed = 10f;

    [Header("Wymuszone kucanie (brak miejsca na wstanie)")]
    public LayerMask ceilingMask = ~0;
    public float ceilingCheckMargin = 0.1f;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Referencje")]
    public Transform cameraTransform;
    public CameraRig cameraRig;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private bool isSprinting;
    private bool isCrouching;
    private float sprintSpinUpTimer;

    private float standHeight;
    private Vector3 standCenter;

    private bool forcedCrouch;

    public bool IsSprintActive => isSprinting;
    public bool IsCrouching => isCrouching; // TYLKO isCrouching, bez forcedCrouch

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraRig == null && cameraTransform != null)
            cameraRig = cameraTransform.GetComponent<CameraRig>();

        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<CameraRig>();

        standHeight = controller.height;
        standCenter = controller.center;
    }

    void Update()
    {
        HandleMovement();
        HandleSprintState();
        HandleCrouchState();
        HandleForcedCrouch();
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

        if (isCrouching || forcedCrouch)
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
        if (isCrouching || forcedCrouch)
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

        if (forcedCrouch)
        {
            if (crouchHeld && !isCrouching)
                isCrouching = true;
            return;
        }

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

    void HandleForcedCrouch()
    {
        if (!isCrouching && !forcedCrouch) return;

        bool hasSpace = CanStandUp();

        if (hasSpace)
        {
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                forcedCrouch = false;
                isCrouching = false;
            }
        }
        else
        {
            forcedCrouch = true;
            isCrouching = true;
        }
    }

    bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (radius + 0.01f);
        Vector3 top = transform.position + Vector3.up * (standHeight - radius - ceilingCheckMargin);

        bool blocked = Physics.CheckCapsule(bottom, top, radius, ceilingMask, QueryTriggerInteraction.Ignore);

        if (debugLogs)
            Debug.Log($"[Player] CanStandUp: bottom={bottom}, top={top}, radius={radius}, blocked={blocked}");

        return !blocked;
    }

    void HandleHeight()
    {
        float desiredHeight;

        if (isCrouching || forcedCrouch)
            desiredHeight = crouchHeight;
        else if (isSprinting)
            desiredHeight = sprintHeight;
        else
            desiredHeight = standHeight;

        float newHeight = Mathf.Lerp(controller.height, desiredHeight, heightChangeSpeed * Time.deltaTime);
        controller.height = newHeight;

        if (Mathf.Approximately(desiredHeight, standHeight))
            controller.center = Vector3.Lerp(controller.center, standCenter, heightChangeSpeed * Time.deltaTime);
        else
            controller.center = new Vector3(0f, newHeight * 0.5f, 0f);

        if (debugLogs)
            Debug.Log($"[Player] height: {controller.height:F2} | isSprint: {isSprinting} | isCrouch: {isCrouching} | forced: {forcedCrouch} | desired: {desiredHeight:F2}");
    }
}
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Prêdkoœci – CHÓD")]
    public float walkForward = 3f;        // W
    public float walkBackward = 1.8f;     // S (wolniej – horror)
    public float walkStrafe = 2.2f;       // A / D (wolniej)

    [Header("Prêdkoœci – BIEG (Shift)")]
    public float sprintForward = 6f;      // W + Shift
    public float sprintBackward = 3.5f;   // S + Shift
    public float sprintStrafe = 4.5f;     // A/D + Shift

    [Header("P³ynnoœæ ruchu")]
    public float acceleration = 8f;       // jak szybko nabiera prêdkoœci
    public float rotationSpeed = 10f;     // szybkoœæ obrotu postaci

    [Header("Start sprintu")]
    [Tooltip("Jak d³ugo gracz rozpêdza siê, zanim osi¹gnie pe³n¹ prêdkoœæ biegu")]
    public float sprintSpinUpTime = 0.5f;

    [Header("Referencje")]
    public Transform cameraTransform;
    public CameraRig cameraRig;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private bool isSprinting;
    private float sprintSpinUpTimer;

    public bool IsSprintActive => isSprinting;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraRig == null && cameraTransform != null)
            cameraRig = cameraTransform.GetComponent<CameraRig>();

        if (cameraRig == null)
            cameraRig = FindObjectOfType<CameraRig>();
    }

    void Update()
    {
        HandleMovement();
        HandleSprintState();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Yaw kamery – kierunek ruchu zgodny z patrzeniem
        float yaw = cameraRig != null ? cameraRig.CurrentYaw : 0f;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

        // --- Postêp rozpêdzania sprintu (0 = chód, 1 = pe³ny bieg) ---
        float sprintProgress = 0f;
        if (isSprinting)
        {
            if (sprintSpinUpTimer > 0f)
                sprintProgress = 1f - (sprintSpinUpTimer / sprintSpinUpTime);
            else
                sprintProgress = 1f;
        }

        // --- Prêdkoœæ zale¿na od kierunku i trybu ---
        float forwardSpeed = Mathf.Lerp(walkForward, sprintForward, sprintProgress);
        float backwardSpeed = Mathf.Lerp(walkBackward, sprintBackward, sprintProgress);
        float strafeSpeed = Mathf.Lerp(walkStrafe, sprintStrafe, sprintProgress);

        // Rozdzielamy sk³adowe: przód/ty³ (v) i boki (h)
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

        // Normalizacja tylko wtedy, gdy idziemy po skosie (¿eby nie sumowaæ prêdkoœci)
        Vector3 targetVel = moveDir.magnitude > 0.01f
            ? Vector3.ClampMagnitude(moveDir, Mathf.Max(forwardSpeed, strafeSpeed, backwardSpeed))
            : Vector3.zero;

        // --- P³ynne przyspieszanie (tylko XZ, Y – grawitacja) ---
        Vector3 horizontalVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        horizontalVel = Vector3.Lerp(horizontalVel, targetVel, acceleration * Time.deltaTime);
        currentVelocity.x = horizontalVel.x;
        currentVelocity.z = horizontalVel.z;

        // --- Obrót postaci w kierunku ruchu (bez obrotu przy cofaniu!) ---
        // Chcemy, ¿eby cofaj¹c gracz patrzy³ nadal do przodu – dlatego ignorujemy S przy obrocie.
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

        // --- Grawitacja ---
        if (!controller.isGrounded)
            currentVelocity.y -= 9.81f;
        else
            currentVelocity.y = -1f;

        controller.Move(currentVelocity * Time.deltaTime);
    }

    void HandleSprintState()
    {
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
}
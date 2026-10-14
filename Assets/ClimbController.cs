using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ClimbController : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Teleport na górê obiektu")]
    [Tooltip("Margines nad powierzchni¹, na którym ma wyl¹dowaæ gracz")]
    public float landingMargin = 0.05f;
    [Tooltip("Z jakiej wysokoœci raycast w dó³ szuka powierzchni")]
    public float landingRayStartHeight = 3f;
    [Tooltip("Jak daleko w dó³ raycast szuka powierzchni")]
    public float landingRayDistance = 10f;

    public bool IsClimbing { get; private set; }

    private CharacterController controller;
    private PlayerController playerController;
    private CameraRig cameraRig;

    private float maxClimbHeight;
    private float climbSpeed;
    private float forwardStep;
    private float wallCheckDistance;

    private Vector3 climbDirection;
    private float startY;

    private enum ClimbPhase { None, Rotating, Climbing, Stepping }
    private ClimbPhase phase = ClimbPhase.None;

    private float stepProgress;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        cameraRig = FindFirstObjectByType<CameraRig>();
    }

    public void StartClimb(float maxHeight, float speed, float fwdStep, float wallDist, Vector3 targetDirection)
    {
        if (IsClimbing) return;

        maxClimbHeight = maxHeight;
        climbSpeed = speed;
        forwardStep = fwdStep;
        wallCheckDistance = wallDist;

        climbDirection = targetDirection;
        climbDirection.y = 0f;
        if (climbDirection.sqrMagnitude < 0.001f)
            climbDirection = transform.forward;
        climbDirection.Normalize();

        startY = transform.position.y;
        stepProgress = 0f;

        if (playerController != null)
            playerController.enabled = false;

        if (cameraRig != null)
            cameraRig.EnterClimbMode(climbDirection);

        IsClimbing = true;
        phase = ClimbPhase.Rotating;

        if (debugLogs)
            Debug.Log($"[Climb] StartClimb | dir: {climbDirection} | startY: {startY:F2}");
    }

    void Update()
    {
        if (!IsClimbing) return;

        switch (phase)
        {
            case ClimbPhase.Rotating:
                HandleRotating();
                break;
            case ClimbPhase.Climbing:
                HandleClimbing();
                break;
            case ClimbPhase.Stepping:
                HandleStepping();
                break;
        }
    }

    void HandleRotating()
    {
        Quaternion targetRot = Quaternion.LookRotation(climbDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRot) < 2f)
        {
            phase = ClimbPhase.Climbing;
            if (debugLogs) Debug.Log("[Climb] Rotating -> Climbing");
        }
    }

    void HandleClimbing()
    {
        Vector3 up = Vector3.up * climbSpeed * Time.deltaTime;
        controller.Move(up);

        Vector3 originLow = transform.position + Vector3.up * (-0.5f);
        Vector3 originMid = transform.position + Vector3.up * 0.2f;

        bool wallLow = Physics.Raycast(originLow, climbDirection, wallCheckDistance);
        bool wallMid = Physics.Raycast(originMid, climbDirection, wallCheckDistance);
        bool wallAhead = wallLow || wallMid;

        bool heightLimit = (transform.position.y - startY) >= maxClimbHeight;

        if (debugLogs)
            Debug.Log($"[Climb] Climbing | Y: {transform.position.y:F2} | wallLow: {wallLow} | wallMid: {wallMid} | heightLimit: {heightLimit}");

        if (!wallAhead || heightLimit)
        {
            phase = ClimbPhase.Stepping;
            stepProgress = 0f;
            if (debugLogs) Debug.Log("[Climb] Climbing -> Stepping");
        }
    }

    void HandleStepping()
    {
        float stepThisFrame = forwardStep * 3f * Time.deltaTime;
        stepThisFrame = Mathf.Min(stepThisFrame, forwardStep - stepProgress);

        Vector3 move = climbDirection * stepThisFrame;
        controller.Move(move);

        stepProgress += stepThisFrame;

        if (debugLogs)
            Debug.Log($"[Climb] Stepping | progress: {stepProgress:F2}/{forwardStep:F2}");

        if (stepProgress >= forwardStep - 0.01f)
        {
            SnapToTopSurface();
            FinishClimb();
        }
    }

    void SnapToTopSurface()
    {
        // Raycast w dó³ z punktu nad graczem – szukamy górnej powierzchni obiektu
        Vector3 rayStart = transform.position + Vector3.up * landingRayStartHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, landingRayDistance))
        {
            // Chcemy, ¿eby DÓ£ CharacterControllera by³ na powierzchni + margines
            // Œrodek CC jest na transform.position, wiêc dolna krawêdŸ = transform.position.y - (height/2)
            float halfHeight = controller.height * 0.5f;
            float targetY = hit.point.y + halfHeight + landingMargin;

            Vector3 newPos = transform.position;
            newPos.y = targetY;

            // Wy³¹czamy CC na chwilê, ¿eby teleport zadzia³a³ natychmiast
            controller.enabled = false;
            transform.position = newPos;
            controller.enabled = true;

            if (debugLogs)
                Debug.Log($"[Climb] SnapToTopSurface | hitY: {hit.point.y:F2} | newY: {targetY:F2}");
        }
        else
        {
            if (debugLogs)
                Debug.Log("[Climb] SnapToTopSurface | raycast w dó³ NIE trafi³ – zostawiam pozycjê jak jest");
        }
    }

    void FinishClimb()
    {
        if (debugLogs) Debug.Log("[Climb] FinishClimb");

        IsClimbing = false;
        phase = ClimbPhase.None;

        if (playerController != null)
            playerController.enabled = true;

        if (cameraRig != null)
            cameraRig.ExitClimbMode();
    }
}
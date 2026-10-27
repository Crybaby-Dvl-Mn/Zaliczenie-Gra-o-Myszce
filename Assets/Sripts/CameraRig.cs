using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [Header("Referencje")]
    public Transform player;
    public Transform cameraSprintAnchor;
    public Renderer[] fadeRenderers;
    public Camera cam;

    [Header("Parametry TPS (chód)")]
    public float tpsDistance = 4f;
    public float tpsHeight = 1.5f;
    public float mouseSensitivity = 3f;

    [Header("Unikanie œcian (TPS)")]
    [Tooltip("Warstwy, które blokuj¹ kamerê (œciany, obiekty)")]
    public LayerMask collisionMask = ~0;
    [Tooltip("Promieñ SphereCast – im wiêkszy, tym wczeœniej kamera siê przybli¿a")]
    public float cameraCollisionRadius = 0.2f;
    [Tooltip("Minimalna odleg³oœæ kamery od gracza, gdy jest œciana")]
    public float minCameraDistance = 0.5f;
    [Tooltip("Margines od œciany – kamera zatrzyma siê trochê przed ni¹")]
    public float collisionMargin = 0.1f;

    [Header("FOV")]
    public float fovTPS = 50f;
    public float fovFPS = 40f;
    public float fovLerpSpeed = 5f;

    [Header("Przejœcie TPS -> FPS")]
    public float transitionSpeed = 5f;
    public float transitionDelay = 0.5f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    public float anchorArriveThreshold = 0.1f;

    [Header("Anchor FPS – relatywny do wysokoœci collidera")]
    public float anchorHeightRatio = 0.8f;
    public float anchorHeightOffset = 0f;

    [Header("Head Bob")]
    public bool bobOnlyWhenSprinting = true;
    public float bobFrequency = 2.2f;
    public float bobVerticalAmplitude = 0.08f;
    public float bobHorizontalAmplitude = 0.05f;
    public float bobBlendSpeed = 6f;

    [Header("Tryb wspinaczki")]
    public float climbLookLimit = 45f;
    public float climbTransitionSpeed = 15f;

    public float CurrentYaw => currentYaw;
    public bool IsFPSReady { get; private set; }

    private float currentYaw;
    private float currentPitch = 20f;
    private float transitionTimer;
    private bool isInSprintView;
    private bool isCrouching;
    private Material[] fadeMaterials;

    private float bobTimer;
    private float bobBlend;
    private Vector3 bobOffset;

    private PlayerController playerController;
    private CharacterController characterController;

    private bool isClimbing;
    private float climbStartYaw;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController = player.GetComponent<PlayerController>();
        characterController = player.GetComponent<CharacterController>();

        if (cam == null)
            cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            cam.fieldOfView = fovTPS;

        if (fadeRenderers == null || fadeRenderers.Length == 0)
            fadeRenderers = player.GetComponentsInChildren<Renderer>();

        fadeMaterials = new Material[fadeRenderers.Length];
        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            fadeMaterials[i] = fadeRenderers[i].material;
            SetMaterialTransparent(fadeMaterials[i]);
        }
    }

    void LateUpdate()
    {
        if (isClimbing)
        {
            HandleClimbLook();
            HandleClimbPosition();
            HandleFOV();
            return;
        }

        HandleMouseLook();
        HandleCameraPosition();
        HandleHeadBob();
        HandleFade();
        HandleFOV();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentYaw += mouseX;

        if (isInSprintView || isCrouching)
        {
            currentPitch = 0f;
        }
        else
        {
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, -50f, 50f);
        }
    }

    void HandleCameraPosition()
    {
        bool shouldSprintView = playerController != null && playerController.IsSprintActive;
        isCrouching = playerController != null && playerController.IsCrouching;

        if (shouldSprintView && !isCrouching && !isInSprintView)
        {
            transitionTimer += Time.deltaTime;
            if (transitionTimer >= transitionDelay)
                isInSprintView = true;
        }
        else if (!shouldSprintView || isCrouching)
        {
            transitionTimer = 0f;
            isInSprintView = false;
        }

        bool useFPS = isInSprintView || isCrouching;

        if (useFPS)
        {
            Vector3 anchorPos = GetAnchorPosition();
            transform.position = Vector3.Lerp(transform.position, anchorPos, transitionSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

            IsFPSReady = Vector3.Distance(transform.position, anchorPos) <= anchorArriveThreshold;
        }
        else
        {
            // TPS z unikaniem œcian
            Quaternion orbitRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 offset = orbitRot * new Vector3(0f, 0f, -tpsDistance);

            // Punkt zaczepienia: nad graczem (np. klatka piersiowa/g³owa)
            Vector3 pivot = player.position + Vector3.up * tpsHeight;

            // Docelowa pozycja kamery (bez kolizji)
            Vector3 desiredPos = pivot + offset;

            // SprawdŸ, czy miêdzy pivotem a desiredPos jest œciana
            Vector3 dir = (desiredPos - pivot).normalized;
            float dist = Vector3.Distance(pivot, desiredPos);

            Vector3 finalPos = desiredPos;

            // SphereCast – wykrywa œciany po drodze
            if (Physics.SphereCast(pivot, cameraCollisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // Kamera zatrzymuje siê przed œcian¹
                float safeDist = Mathf.Max(hit.distance - collisionMargin, minCameraDistance);
                finalPos = pivot + dir * safeDist;
            }

            // P³ynne przybli¿anie do finalPos
            transform.position = Vector3.Lerp(transform.position, finalPos, transitionSpeed * 2f * Time.deltaTime);

            // Rotacja: patrz na pivot
            Quaternion lookRot = Quaternion.LookRotation(pivot - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);

            IsFPSReady = false;
        }
    }

    Vector3 GetAnchorPosition()
    {
        if (characterController == null)
            return cameraSprintAnchor.position;

        Vector3 basePos = cameraSprintAnchor.position;
        float newY = player.position.y + (characterController.height * anchorHeightRatio) + anchorHeightOffset;

        return new Vector3(basePos.x, newY, basePos.z);
    }

    void HandleFOV()
    {
        if (cam == null) return;

        float targetFov = (isInSprintView || isCrouching) ? fovFPS : fovTPS;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
    }

    public void EnterClimbMode(Vector3 climbDirection)
    {
        isClimbing = true;
        climbStartYaw = currentYaw;
    }

    public void ExitClimbMode()
    {
        isClimbing = false;
    }

    void HandleClimbLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentYaw += mouseX;
        currentPitch -= mouseY;

        float yawDelta = Mathf.DeltaAngle(climbStartYaw, currentYaw);
        yawDelta = Mathf.Clamp(yawDelta, -climbLookLimit, climbLookLimit);
        currentYaw = climbStartYaw + yawDelta;

        currentPitch = Mathf.Clamp(currentPitch, -40f, 70f);
    }

    void HandleClimbPosition()
    {
        Vector3 anchorPos = GetAnchorPosition();
        transform.position = Vector3.Lerp(transform.position, anchorPos, climbTransitionSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        IsFPSReady = true;
    }

    void HandleHeadBob()
    {
        if (playerController == null) return;

        if (isCrouching)
        {
            bobBlend = 0f;
            bobOffset = Vector3.zero;
            return;
        }

        float horizontalSpeed = new Vector3(
            playerController.GetComponent<CharacterController>().velocity.x,
            0f,
            playerController.GetComponent<CharacterController>().velocity.z
        ).magnitude;

        bool bobActive = bobOnlyWhenSprinting
            ? playerController.IsSprintActive
            : horizontalSpeed > 0.5f;

        if (!bobActive)
        {
            bobBlend = 0f;
            bobOffset = Vector3.zero;
            return;
        }

        bobBlend = Mathf.Lerp(bobBlend, 1f, bobBlendSpeed * Time.deltaTime);

        float speedFactor = Mathf.Clamp01(horizontalSpeed / 6f);
        bobTimer += Time.deltaTime * bobFrequency * Mathf.PI * 2f * (0.7f + 0.6f * speedFactor);

        float vertical = Mathf.Sin(bobTimer * 2f) * bobVerticalAmplitude;
        float horizontal = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;

        Vector3 camRight = transform.right;
        Vector3 camUp = transform.up;

        bobOffset = (camUp * vertical + camRight * horizontal) * bobBlend;

        transform.position += bobOffset;
    }

    void HandleFade()
    {
        if (fadeMaterials == null || fadeMaterials.Length == 0) return;

        float dist = Vector3.Distance(transform.position - bobOffset, player.position);
        float normalized = Mathf.Clamp01(dist / tpsDistance);
        float alpha = fadeCurve.Evaluate(normalized);

        if (isInSprintView || isCrouching)
            alpha = 0f;

        foreach (var mat in fadeMaterials)
        {
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    void SetMaterialTransparent(Material mat)
    {
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Mode", 2f);
    }
}
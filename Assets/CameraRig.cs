using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [Header("Referencje")]
    public Transform player;
    public Transform cameraSprintAnchor;
    public Renderer[] fadeRenderers;
    public Camera cam; // referencja do Camera (jeœli puste – auto)

    [Header("Parametry TPS")]
    public float tpsDistance = 4f;
    public float tpsHeight = 1.5f;
    public float mouseSensitivity = 3f;
    [Tooltip("Maksymalny k¹t patrzenia w górê/dó³ w TPS")]
    public float tpsPitchLimit = 50f;

    [Header("FOV")]
    public float fovTPS = 50f;
    public float fovFPS = 40f;
    [Tooltip("Jak szybko FOV zmienia siê miêdzy trybami")]
    public float fovLerpSpeed = 5f;

    [Header("Przejœcie TPS -> FPS")]
    public float transitionSpeed = 5f;
    public float transitionDelay = 0.5f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

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

    private float currentYaw;
    private float currentPitch = 20f;
    private float transitionTimer;
    private bool isInSprintView;
    private Material[] fadeMaterials;

    private float bobTimer;
    private float bobBlend;
    private Vector3 bobOffset;

    private PlayerController playerController;

    private bool isClimbing;
    private float climbStartYaw;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController = player.GetComponent<PlayerController>();

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

        if (isInSprintView)
        {
            // FPS – ca³kowita blokada pitcha (sztywno 0)
            currentPitch = 0f;
        }
        else
        {
            // TPS – pe³ny zakres ±tpsPitchLimit
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, -tpsPitchLimit, tpsPitchLimit);
        }
    }

    void HandleCameraPosition()
    {
        bool shouldSprintView = playerController != null && playerController.IsSprintActive;

        if (shouldSprintView && !isInSprintView)
        {
            transitionTimer += Time.deltaTime;
            if (transitionTimer >= transitionDelay)
                isInSprintView = true;
        }
        else if (!shouldSprintView)
        {
            transitionTimer = 0f;
            isInSprintView = false;
        }

        if (isInSprintView)
        {
            // FPS – kamera na anchorze, rotacja tylko yaw (pitch = 0)
            transform.position = Vector3.Lerp(transform.position, cameraSprintAnchor.position, transitionSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }
        else
        {
            // TPS – orbita wokó³ gracza, pitch wp³ywa na k¹t pionowy orbity
            Quaternion orbitRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 offset = orbitRot * new Vector3(0f, 0f, -tpsDistance);
            Vector3 tpsPos = player.position + Vector3.up * tpsHeight + offset;

            transform.position = Vector3.Lerp(transform.position, tpsPos, transitionSpeed * 0.5f * Time.deltaTime);

            // Kamera patrzy na gracza
            Vector3 lookTarget = player.position + Vector3.up * tpsHeight;
            Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 8f * Time.deltaTime);
        }
    }

    void HandleFOV()
    {
        if (cam == null) return;

        float targetFov = isInSprintView ? fovFPS : fovTPS;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
    }

    // --- TRYB WSPINACZKI ---

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
        transform.position = Vector3.Lerp(transform.position, cameraSprintAnchor.position, climbTransitionSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    // --- HEAD BOB ---

    void HandleHeadBob()
    {
        if (playerController == null) return;

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

        if (isInSprintView)
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
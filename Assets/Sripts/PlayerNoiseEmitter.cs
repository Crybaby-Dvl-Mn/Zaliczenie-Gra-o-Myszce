using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("DŸwiêk – CHÓD (lekki)")]
    public float walkNoiseInterval = 1.5f;
    public float walkNoiseRadius = 6f;

    [Header("DŸwiêk – BIEG (g³oœny)")]
    public float sprintNoiseInterval = 0.4f;
    public float sprintNoiseRadius = 14f;

    [Header("DŸwiêk – KUCANIE (cichy)")]
    public float crouchNoiseInterval = 2.0f;
    public float crouchNoiseRadius = 3f;

    [Header("DŸwiêk – INTERAKCJA (g³oœny, jednorazowy)")]
    public float interactionNoiseRadius = 10f;

    private PlayerController playerController;
    private CharacterController characterController;
    private float noiseTimer;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (playerController == null || characterController == null) return;

        float speed = new Vector3(
            characterController.velocity.x,
            0f,
            characterController.velocity.z
        ).magnitude;

        if (speed < 0.5f) return;

        noiseTimer += Time.deltaTime;

        float interval, radius;

        if (playerController.IsCrouching)
        {
            interval = crouchNoiseInterval;
            radius = crouchNoiseRadius;
        }
        else if (playerController.IsSprintActive)
        {
            interval = sprintNoiseInterval;
            radius = sprintNoiseRadius;
        }
        else
        {
            interval = walkNoiseInterval;
            radius = walkNoiseRadius;
        }

        if (noiseTimer >= interval)
        {
            noiseTimer = 0f;
            EmitNoise(radius);
        }
    }

    public void EmitInteractionNoise()
    {
        EmitNoise(interactionNoiseRadius);
    }

    void EmitNoise(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyStalker>();
            if (enemy != null)
            {
                bool loud = radius >= sprintNoiseRadius;
                enemy.HearNoise(transform.position, loud);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, walkNoiseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sprintNoiseRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, crouchNoiseRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionNoiseRadius);
    }
}
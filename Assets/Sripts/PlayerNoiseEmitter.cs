using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("DŸwiêk – CHÓD (lekki)")]
    public float walkNoiseInterval = 1.5f;
    public float walkNoiseRadius = 6f;

    [Header("DŸwiêk – BIEG (g³oœny)")]
    public float sprintNoiseInterval = 0.4f;
    public float sprintNoiseRadius = 14f;

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

        bool sprinting = playerController.IsSprintActive;
        float interval = sprinting ? sprintNoiseInterval : walkNoiseInterval;
        float radius = sprinting ? sprintNoiseRadius : walkNoiseRadius;

        if (noiseTimer >= interval)
        {
            noiseTimer = 0f;
            EmitNoise(radius, sprinting);
        }
    }

    // Wywo³aj to przy interakcji (np. otwieranie drzwi, podnoszenie przedmiotu)
    public void EmitInteractionNoise()
    {
        EmitNoise(interactionNoiseRadius, true);
    }

    void EmitNoise(float radius, bool loud)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyStalker>();
            if (enemy != null)
            {
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionNoiseRadius);
    }
}
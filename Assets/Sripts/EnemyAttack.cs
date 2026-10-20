using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Atak")]
    public float attackRange = 1.8f;
    public float attackDamage = 25f;
    public float attackCooldown = 1.5f;

    [Header("Og³uszenie po ataku")]
    [Tooltip("Ile sekund przeciwnik stoi w miejscu po zadaniu obra¿eñ")]
    public float attackStunDuration = 10f;

    public bool IsStunned { get; private set; }

    private float lastAttackTime = -999f;
    private float stunEndTime = -1f;

    private PlayerHealth playerHealth;
    private Transform player;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        // Sprawdzenie koñca og³uszenia
        if (IsStunned && Time.time >= stunEndTime)
        {
            IsStunned = false;
        }

        if (player == null || playerHealth == null) return;
        if (IsStunned) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            playerHealth.TakeDamage(attackDamage);

            // Og³uszenie
            IsStunned = true;
            stunEndTime = Time.time + attackStunDuration;
        }
    }
}
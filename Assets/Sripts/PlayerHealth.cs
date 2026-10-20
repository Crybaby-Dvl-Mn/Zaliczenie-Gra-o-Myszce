using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Debug")]
    public bool debugLogs = true;

    public bool IsDead => currentHealth <= 0f;

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        if (debugLogs)
            Debug.Log($"[PlayerHealth] Otrzymano {amount} dmg | HP: {currentHealth:F0}/{maxHealth}");

        if (IsDead)
        {
            if (debugLogs) Debug.Log("[PlayerHealth] GRACZ NIE ¯YJE");
            // Tu mo¿esz dodaæ Game Over
        }
    }
}
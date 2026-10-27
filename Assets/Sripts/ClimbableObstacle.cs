using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class ClimbableObstacle : MonoBehaviour
{
    [Tooltip("Czy ten obiekt mo¿e byæ wy³¹czony podczas poœcigu (¿eby przeciwnik przez niego przenikn¹³)")]
    public bool allowPassThroughInChase = true;

    private NavMeshObstacle obstacle;
    private bool originalCarve;

    void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        originalCarve = obstacle.carving;
    }

    // Wywo³ywane przez EnemyStalker, gdy przeciwnik wchodzi w poœcig
    public void DisableForChase()
    {
        if (!allowPassThroughInChase) return;
        if (obstacle == null) return;

        obstacle.carving = false;
    }

    // Wywo³ywane przez EnemyStalker, gdy poœcig siê koñczy
    public void RestoreAfterChase()
    {
        if (obstacle == null) return;

        obstacle.carving = originalCarve;
    }
}
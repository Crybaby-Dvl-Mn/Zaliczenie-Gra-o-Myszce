using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    [Tooltip("Waga punktu – wiêksza = wa¿niejszy, czêœciej wybierany")]
    public float weight = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}
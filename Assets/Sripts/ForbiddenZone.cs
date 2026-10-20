using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ForbiddenZone : MonoBehaviour
{
    [Tooltip("Kolor gizmo w Scene view")]
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.25f);

    private Collider zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    public bool ContainsPoint(Vector3 point)
    {
        if (zoneCollider == null) return false;
        return zoneCollider.bounds.Contains(point);
    }

    void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.bounds.center - transform.position, col.bounds.size);

        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
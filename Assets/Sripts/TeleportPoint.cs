using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BoxCollider))]
public class TeleportPoint : MonoBehaviour
{
    [Header("Polaczenie z drugim punktem")]
    [Tooltip("Punkt wyjsciowy po drugiej stronie przeszkody")]
    public TeleportPoint pairedPoint;

    [Header("Ustawienia spowolnienia po teleportacji")]
    [Tooltip("Mnoznik predkosci przeciwnika po teleportacji (np. 0.5 = 50% predkosci)")]
    public float slowMultiplier = 0.5f;
    [Tooltip("Czas trwania spowolnienia w sekundach")]
    public float slowDuration = 2.0f;

    [Header("Cooldown")]
    [Tooltip("Czas w sekundach, po ktorym punkt moze znowu przeteleportowac przeciwnika (zapobiega zapetleniu)")]
    public float cooldown = 2.0f;

    [Header("Warunki")]
    [Tooltip("Czy teleportowac tylko wtedy, gdy przeciwnik jest w stanie poscigu (Chase / ChaseWindup)")]
    public bool teleportOnlyInChase = true;

    [Header("Debug")]
    public bool debugLogs = true;

    private float lastTeleportTime = -999f;
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
            boxCollider.isTrigger = true;
    }

    void Reset()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (pairedPoint == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[TeleportPoint] '{name}' nie ma przypisanego pairedPoint!");
            return;
        }

        // Sprawdz cooldown (zeby nie teleportowac w kolko)
        if (Time.time < lastTeleportTime + cooldown)
            return;

        // Szukaj EnemyStalker na dotknietym obiekcie lub jego rodzicu
        var stalker = other.GetComponent<EnemyStalker>();
        if (stalker == null)
            stalker = other.GetComponentInParent<EnemyStalker>();

        if (stalker == null)
            return;

        // Opcjonalne ograniczenie: teleport tylko w poscigu
        if (teleportOnlyInChase)
        {
            bool inChase = stalker.CurrentState == EnemyStalker.State.Chase ||
                           stalker.CurrentState == EnemyStalker.State.ChaseWindup;
            if (!inChase)
                return;
        }

        ExecuteTeleport(stalker);
    }

    void ExecuteTeleport(EnemyStalker stalker)
    {
        // Ustaw cooldown na obu powiazanych punktach, aby wrog nie wrocil od razu
        float now = Time.time;
        lastTeleportTime = now;
        pairedPoint.lastTeleportTime = now;

        Vector3 targetPos = pairedPoint.transform.position;

        // Upewnij sie, ze pozycja lezy na NavMesh
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            targetPos = hit.position;
        }

        stalker.TeleportTo(targetPos);
        stalker.ApplyTemporarySlow(slowDuration, slowMultiplier);

        if (debugLogs)
        {
            Debug.Log($"[TeleportPoint] Przeteleportowano {stalker.name} z '{name}' do '{pairedPoint.name}'. " +
                      $"Pozycja: {targetPos}, Spowolnienie: x{slowMultiplier} na {slowDuration}s");
        }
    }

    void OnDrawGizmos()
    {
        // Rysuj obrys triggera
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // Rysuj linie laczaca punkty
        if (pairedPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, pairedPoint.transform.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
        else
        {
            // Brak pary - ostrzezenie wizualne
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
    }
}

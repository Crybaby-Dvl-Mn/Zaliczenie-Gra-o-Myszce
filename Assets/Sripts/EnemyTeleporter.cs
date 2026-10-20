using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTeleporter : MonoBehaviour
{
    [Header("Teleportacja")]
    [Tooltip("Jeœli przeciwnik jest dalej ni¿ to od gracza – teleportuj")]
    public float teleportTriggerDistance = 35f;
    [Tooltip("Na jak¹ odleg³oœæ od gracza teleportowaæ (min)")]
    public float teleportMinDistance = 18f;
    [Tooltip("Na jak¹ odleg³oœæ od gracza teleportowaæ (max)")]
    public float teleportMaxDistance = 25f;
    [Tooltip("Minimalna odleg³oœæ od gracza, ¿eby nie pojawi³ siê tu¿ obok")]
    public float teleportSafetyDistance = 15f;

    [Header("Widocznoœæ")]
    [Tooltip("K¹t sto¿ka widzenia kamery, w którym przeciwnik nie mo¿e siê pojawiæ")]
    public float cameraFovAngle = 70f;
    [Tooltip("Zasiêg widzenia kamery (raycast)")]
    public float cameraSightRange = 60f;

    [Header("Próby")]
    public int maxAttempts = 10;

    [Header("Debug")]
    public bool debugLogs = true;

    private Transform player;
    private Camera playerCamera;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > teleportTriggerDistance)
        {
            TryTeleport();
        }
    }

    void TryTeleport()
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = PickCandidate();

            if (candidate == Vector3.zero) continue;
            if (Vector3.Distance(candidate, player.position) < teleportSafetyDistance) continue;
            if (IsVisibleToPlayer(candidate)) continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                Teleport(hit.position);
                return;
            }
        }

        if (debugLogs)
            Debug.Log("[Teleporter] Nie uda³o siê znaleŸæ miejsca – zostajê na miejscu");
    }

    Vector3 PickCandidate()
    {
        // Losowy kierunek i odleg³oœæ od gracza
        float angle = Random.value * Mathf.PI * 2f;
        float dist = Random.Range(teleportMinDistance, teleportMaxDistance);

        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return player.position + dir * dist;
        
        // Nowa metoda:
        bool IsCandidateInForbiddenZone(Vector3 pos)
        {
            ForbiddenZone[] zones = FindObjectsByType<ForbiddenZone>(FindObjectsSortMode.None);
            foreach (var z in zones)
            {
                if (z != null && z.ContainsPoint(pos)) return true;
            }
            return false;
        }
    }

    bool IsVisibleToPlayer(Vector3 pos)
    {
        if (playerCamera == null) return false;

        Vector3 camPos = playerCamera.transform.position;
        Vector3 camForward = playerCamera.transform.forward;

        // Wektor od kamery do pozycji
        Vector3 toPos = pos - camPos;

        // SprawdŸ k¹t w sto¿ku widzenia
        float angle = Vector3.Angle(camForward, toPos);
        if (angle > cameraFovAngle * 0.5f) return false; // poza sto¿kiem – niewidoczny

        // Raycast – czy nic nie zas³ania
        if (Physics.Raycast(camPos, toPos.normalized, out RaycastHit hit, toPos.magnitude))
        {
            // Jeœli trafi³ coœ innego ni¿ grunt – zas³oniête
            if (hit.collider.gameObject != gameObject && hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground"))
                return false;
        }

        // W zasiêgu, w sto¿ku, bez przeszkód – widoczny
        return true;
    }

    void Teleport(Vector3 pos)
    {
        agent.enabled = false;
        transform.position = pos;
        agent.enabled = true;

        if (debugLogs)
            Debug.Log($"[Teleporter] Teleport na {pos} | dystans od gracza: {Vector3.Distance(pos, player.position):F1}");
    }
}
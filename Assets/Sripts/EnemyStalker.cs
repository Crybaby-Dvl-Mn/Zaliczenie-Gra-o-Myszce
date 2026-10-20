using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyStalker : MonoBehaviour
{
    [Header("Patrol – strefy wokó³ gracza")]
    public float patrolRadius = 25f;
    public float wanderRadius = 20f;
    public float wanderMinDistance = 8f;
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 2f;

    [Header("S³uch")]
    public float hearingRadius = 15f;
    public float investigateTimeLight = 5f;
    public float investigateTimeLoud = 3f;

    [Header("Zapach / wyczucie")]
    public float smellRadius = 8f;

    [Header("Poœcig")]
    public float chaseRadius = 6f;
    public float chaseSpeed = 5.5f;
    public float chaseGiveUpRadius = 20f;

    [Header("Prêdkoœci – investigate")]
    public float investigateWalkSpeed = 2.5f;
    public float investigateRunSpeed = 5f;

    [Header("Strefy zakazane")]
    [Tooltip("Jak blisko granicy strefy ma siê zatrzymaæ przeciwnik (m)")]
    public float forbiddenZoneMargin = 1.5f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerController;
    private EnemyAttack enemyAttack;
    private List<ForbiddenZone> forbiddenZones = new List<ForbiddenZone>();

    public enum State { Patrol, Investigate, Chase, Search, BlockedByZone }
    public State CurrentState { get; private set; } = State.Patrol;

    private Vector3 lastKnownPlayerPos;
    private float stateTimer;
    private float investigateDuration;
    private Vector3 investigateTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAttack = GetComponent<EnemyAttack>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        // ZnajdŸ wszystkie strefy zakazane w scenie
        forbiddenZones.Clear();
        forbiddenZones.AddRange(FindObjectsByType<ForbiddenZone>(FindObjectsSortMode.None));

        agent.speed = patrolSpeed;
        PickNextPatrolTarget();
    }

    void Update()
    {
        if (player == null) return;

        // OG£USZENIE – stój w miejscu
        if (enemyAttack != null && enemyAttack.IsStunned)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // STREFA ZAKAZANA – jeœli gracz w strefie, przeciwnik nie œciga
        bool playerInForbidden = IsPointInAnyZone(player.position);

        if (playerInForbidden)
        {
            // Zatrzymaj siê, jeœli próbujesz wejœæ w strefê lub œcigaæ gracza w strefie
            if (CurrentState == State.Chase || CurrentState == State.Investigate)
            {
                CurrentState = State.BlockedByZone;
                if (agent.hasPath) agent.ResetPath();
            }
            else if (CurrentState == State.Patrol)
            {
                // Kontynuuj patrol, ale nie wchodŸ w strefy
                if (agent.hasPath && IsPointInAnyZone(agent.destination))
                {
                    if (agent.hasPath) agent.ResetPath();
                    stateTimer = 0f;
                    PickNextPatrolTarget();
                }
            }

            // W stanie BlockedByZone – stój i czekaj
            if (CurrentState == State.BlockedByZone)
            {
                return;
            }
        }
        else
        {
            // Gracz poza stref¹ – wyjdŸ z BlockedByZone
            if (CurrentState == State.BlockedByZone)
            {
                CurrentState = State.Patrol;
                stateTimer = 0f;
                PickNextPatrolTarget();
            }
        }

        // CHASE – ponad wszystko, ale nie jeœli gracz w strefie
        if (!playerInForbidden && distToPlayer <= chaseRadius && CurrentState != State.Chase)
        {
            EnterChase();
        }

        // ZAPACH – tylko jeœli gracz poza stref¹
        if (!playerInForbidden && CurrentState != State.Chase && distToPlayer <= smellRadius)
        {
            lastKnownPlayerPos = player.position;
            if (CurrentState != State.Investigate || investigateTarget != player.position)
            {
                EnterInvestigate(player.position, investigateTimeLight, investigateWalkSpeed);
            }
        }

        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
            case State.BlockedByZone: /* stoi, nic nie robi */ break;
        }
    }

    // --- SPRAWDZANIE STREF ---

    bool IsPointInAnyZone(Vector3 point)
    {
        foreach (var zone in forbiddenZones)
        {
            if (zone != null && zone.ContainsPoint(point))
                return true;
        }
        return false;
    }

    // Sprawdza, czy punkt jest blisko granicy strefy (do zatrzymania siê przed wejœciem)
    bool IsPointNearZone(Vector3 point)
    {
        foreach (var zone in forbiddenZones)
        {
            if (zone == null) continue;
            var col = zone.GetComponent<Collider>();
            if (col == null) continue;

            Vector3 closest = col.ClosestPoint(point);
            if (Vector3.Distance(closest, point) < forbiddenZoneMargin)
                return true;
        }
        return false;
    }

    // --- PATROL ---

    void UpdatePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= patrolWaitTime)
            {
                stateTimer = 0f;
                PickNextPatrolTarget();
            }
        }
    }

    void PickNextPatrolTarget()
    {
        PatrolPoint[] allPoints = FindObjectsByType<PatrolPoint>(FindObjectsSortMode.None);
        List<PatrolPoint> valid = new List<PatrolPoint>();

        foreach (var p in allPoints)
        {
            if (Vector3.Distance(p.transform.position, player.position) <= patrolRadius
                && !IsPointInAnyZone(p.transform.position))
            {
                valid.Add(p);
            }
        }

        agent.speed = patrolSpeed;

        if (valid.Count > 0)
        {
            PatrolPoint chosen = PickWeighted(valid);
            agent.SetDestination(chosen.transform.position);
        }
        else
        {
            PickWanderTarget();
            agent.SetDestination(agent.destination);
        }
    }

    PatrolPoint PickWeighted(List<PatrolPoint> points)
    {
        float total = 0f;
        foreach (var p in points) total += p.weight;
        float r = Random.value * total;
        float acc = 0f;
        foreach (var p in points)
        {
            acc += p.weight;
            if (r <= acc) return p;
        }
        return points[points.Count - 1];
    }

    void PickWanderTarget()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 rand = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = player.position + new Vector3(rand.x, 0f, rand.y);

            if (Vector3.Distance(candidate, player.position) < wanderMinDistance) continue;
            if (IsPointInAnyZone(candidate)) continue;
            if (IsPointNearZone(candidate)) continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
        // Brak miejsca – stój
        if (agent.hasPath) agent.ResetPath();
    }

    // --- INVESTIGATE ---

    public void HearNoise(Vector3 noisePos, bool loud)
    {
        if (CurrentState == State.Chase || CurrentState == State.BlockedByZone) return;
        if (enemyAttack != null && enemyAttack.IsStunned) return;
        if (Vector3.Distance(transform.position, noisePos) > hearingRadius) return;
        if (IsPointInAnyZone(noisePos)) return; // dŸwiêk z wnêtrza strefy ignorujemy

        lastKnownPlayerPos = noisePos;
        EnterInvestigate(noisePos, loud ? investigateTimeLoud : investigateTimeLight,
                        loud ? investigateRunSpeed : investigateWalkSpeed);
    }

    void EnterInvestigate(Vector3 target, float duration, float speed)
    {
        CurrentState = State.Investigate;
        stateTimer = 0f;
        investigateTarget = target;
        investigateDuration = duration;
        agent.speed = speed;

        // ZnajdŸ bezpieczny punkt na granicy strefy, jeœli target jest w strefie
        Vector3 safeTarget = target;
        if (IsPointInAnyZone(target))
        {
            // Cofnij siê – idŸ do najbli¿szego punktu na zewn¹trz
            safeTarget = FindSafePointOutsideZones(target);
        }
        agent.SetDestination(safeTarget);
    }

    Vector3 FindSafePointOutsideZones(Vector3 inside)
    {
        // Szukaj punktu na zewn¹trz stref, w stronê od gracza do przeciwnika
        Vector3 dir = (transform.position - player.position).normalized;
        for (int i = 1; i <= 10; i++)
        {
            Vector3 candidate = inside + dir * (i * 0.5f);
            if (!IsPointInAnyZone(candidate) && NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                return hit.position;
        }
        return transform.position;
    }

    void UpdateInvestigate()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= investigateDuration)
            {
                CurrentState = State.Patrol;
                stateTimer = 0f;
                PickNextPatrolTarget();
            }
        }
    }

    // --- CHASE ---

    void EnterChase()
    {
        CurrentState = State.Chase;
        agent.speed = chaseSpeed;
    }

    void UpdateChase()
    {
        // Jeœli gracz wszed³ w strefê – przerwij poœcig
        if (IsPointInAnyZone(player.position))
        {
            CurrentState = State.BlockedByZone;
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > chaseGiveUpRadius)
        {
            CurrentState = State.Search;
            stateTimer = 0f;
            agent.speed = investigateWalkSpeed;
            agent.SetDestination(lastKnownPlayerPos);
        }
    }

    // --- SEARCH ---

    void UpdateSearch()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= investigateTimeLight)
            {
                CurrentState = State.Patrol;
                stateTimer = 0f;
                PickNextPatrolTarget();
            }
        }
    }
}
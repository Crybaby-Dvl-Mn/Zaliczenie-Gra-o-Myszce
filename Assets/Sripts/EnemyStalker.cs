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

    [Header("OpóŸnienie wejœcia w Chase")]
    [Tooltip("Ile sekund przeciwnik stoi w miejscu po wykryciu gracza, zanim zacznie biec")]
    public float chaseWindupTime = 5f;

    [Header("Prêdkoœci – investigate")]
    public float investigateWalkSpeed = 2.5f;
    public float investigateRunSpeed = 5f;

    [Header("Strefy zakazane")]
    public float forbiddenZoneMargin = 1.5f;

    [Header("Spowolnienie podczas animacji gracza")]
    public float playerAnimSlowMultiplier = 0.3f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerController playerController;
    private ClimbController climbController;
    private EnemyAttack enemyAttack;
    private List<ForbiddenZone> forbiddenZones = new List<ForbiddenZone>();

    public enum State { Patrol, Investigate, Chase, ChaseWindup, Search, BlockedByZone }
    public State CurrentState { get; private set; } = State.Patrol;

    private Vector3 lastKnownPlayerPos;
    private float stateTimer;
    private float investigateDuration;
    private Vector3 investigateTarget;

    private float chaseWindupTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAttack = GetComponent<EnemyAttack>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            climbController = playerObj.GetComponent<ClimbController>();
        }

        forbiddenZones.Clear();
        forbiddenZones.AddRange(FindObjectsByType<ForbiddenZone>(FindObjectsSortMode.None));

        agent.speed = patrolSpeed;
        PickNextPatrolTarget();
    }

    void Update()
    {
        if (player == null) return;

        // OG£USZENIE
        if (enemyAttack != null && enemyAttack.IsStunned)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        ApplySpeedMultiplier();

        bool playerInForbidden = IsPointInAnyZone(player.position);

        if (playerInForbidden)
        {
            if (CurrentState == State.Chase || CurrentState == State.ChaseWindup || CurrentState == State.Investigate)
            {
                CurrentState = State.BlockedByZone;
                if (agent.hasPath) agent.ResetPath();
            }
            else if (CurrentState == State.Patrol)
            {
                if (agent.hasPath && IsPointInAnyZone(agent.destination))
                {
                    if (agent.hasPath) agent.ResetPath();
                    stateTimer = 0f;
                    PickNextPatrolTarget();
                }
            }

            if (CurrentState == State.BlockedByZone)
                return;
        }
        else
        {
            if (CurrentState == State.BlockedByZone)
            {
                CurrentState = State.Patrol;
                stateTimer = 0f;
                PickNextPatrolTarget();
            }
        }

        // Detekcja Chase
        if (!playerInForbidden && distToPlayer <= chaseRadius &&
            CurrentState != State.Chase && CurrentState != State.ChaseWindup)
        {
            EnterChaseWindup();
        }

        // Zapach
        if (!playerInForbidden && CurrentState != State.Chase && CurrentState != State.ChaseWindup &&
            distToPlayer <= smellRadius)
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
            case State.ChaseWindup: UpdateChaseWindup(); break;
            case State.Search: UpdateSearch(); break;
            case State.BlockedByZone: break;
        }
    }

    // --- CHASE WINDUP (5 s stania przed biegiem) ---

    void EnterChaseWindup()
    {
        CurrentState = State.ChaseWindup;
        chaseWindupTimer = chaseWindupTime;
        if (agent.hasPath) agent.ResetPath();
    }

    void UpdateChaseWindup()
    {
        chaseWindupTimer -= Time.deltaTime;

        // Przeciwnik stoi w miejscu, ale patrzy na gracza
        Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
        if (lookTarget != transform.position)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 5f * Time.deltaTime);
        }

        if (chaseWindupTimer <= 0f)
        {
            CurrentState = State.Chase;
        }
    }

    // --- SPOWOLNIENIE ---

    void ApplySpeedMultiplier()
    {
        float mult = 1f;
        if (climbController != null && climbController.IsClimbing)
            mult = playerAnimSlowMultiplier;

        float baseSpeed = patrolSpeed;
        switch (CurrentState)
        {
            case State.Patrol: baseSpeed = patrolSpeed; break;
            case State.Investigate: baseSpeed = investigateWalkSpeed; break;
            case State.Chase: baseSpeed = chaseSpeed; break;
            case State.ChaseWindup: baseSpeed = 0f; break;
            case State.Search: baseSpeed = investigateWalkSpeed; break;
        }

        agent.speed = baseSpeed * mult;
    }

    // --- STREFY ---

    bool IsPointInAnyZone(Vector3 point)
    {
        foreach (var zone in forbiddenZones)
            if (zone != null && zone.ContainsPoint(point))
                return true;
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

        if (valid.Count > 0)
        {
            PatrolPoint chosen = PickWeighted(valid);
            agent.SetDestination(chosen.transform.position);
        }
        else
        {
            PickWanderTarget();
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

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
        if (agent.hasPath) agent.ResetPath();
    }

    // --- INVESTIGATE ---

    public void HearNoise(Vector3 noisePos, bool loud)
    {
        if (CurrentState == State.Chase || CurrentState == State.ChaseWindup || CurrentState == State.BlockedByZone) return;
        if (enemyAttack != null && enemyAttack.IsStunned) return;
        if (Vector3.Distance(transform.position, noisePos) > hearingRadius) return;
        if (IsPointInAnyZone(noisePos)) return;

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

        Vector3 safeTarget = target;
        if (IsPointInAnyZone(target))
            safeTarget = FindSafePointOutsideZones(target);

        agent.SetDestination(safeTarget);
    }

    Vector3 FindSafePointOutsideZones(Vector3 inside)
    {
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

    void UpdateChase()
    {
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
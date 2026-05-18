using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class PatrolRoom
{
    public string roomName;
    public Transform doorApproach;
    public Door door;
    public Transform doorApproach2;
    public Door door2;
    public Transform interiorPoint;
    [Tooltip("XZ size of the room rectangle. Set this to match the room's floor dimensions.")]
    public Vector3 roomSize = new Vector3(6f, 3f, 6f);
    [HideInInspector] public float visitCooldownRemaining;
}

public class BossEnemyPatrol : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Jumpscare,
        EnterRoom,
        SearchRoom,
        ExitRoom
    }

    [Header("Roam Settings")]
    public float roamRadius = 12f;

    [Header("Speeds")]
    public float patrolSpeed = 2.5f;
    public float patrolChaseSpeed = 4.5f;
    public float returnSpeed = 3f;
    public float rotationSpeed = 8f;

    [Header("Patrol Detection")]
    public float patrolDetectionRadius = 9f;
    public float closeDetectionRadius = 2f;

    [Header("Patrol Attack")]
    public float patrolAttackRadius = 2.2f;

    [Header("Attack Settings")]
    public float chargeTime = 1.2f;
    public float attackSpeed = 8f;
    public float attackDuration = 0.6f;
    public float attackCooldown = 2f;
    [Range(0f, 1f)]
    public float attackDamagePercent = 0.2f;
    public float faceDistance = 0.7f;
    public float attackOffset = 0f;
    public float idleTime = 2f;

    [Header("Jumpscare Settings")]
    public float jumpscareDuration = 2f;
    public float jumpscareShakeIntensity = 1f;

    [Header("Room Patrol")]
    public PatrolRoom[] patrolRooms;
    [Range(0f, 1f)] public float roomVisitChance = 0.4f;
    public float roomVisitCooldown = 30f;
    public float roomSearchTime = 3.5f;
    public float doorOpenDistance = 2f;

    [Header("After Attack")]
    public float ignorePlayerAfterAttackTime = 5f;
    private float ignorePlayerTimer;

    private PlayerMovement player;
    private EnemyState currentState;

    private Vector3 targetPosition;
    private Vector3 attackTargetPosition;
    private Vector3 roamCenter;
    private bool isOnCooldown;

    private NavMeshAgent agent;

    private float chargeTimer;
    private float attackTimer;
    private float cooldownTimer;
    private float idleTimer;
    private float jumpscareTimer;

    private Vector3 savedPatternPosition;
    private Quaternion savedPatternRotation;
    private Vector3 savedTargetPosition;

    private PatrolRoom currentRoom;
    private float searchTimer;
    private bool reachedDoor;
    private bool reachedExitDoor;

    public EnemyState CurrentState => currentState;
    public string CurrentRoomName => currentRoom?.roomName ?? "—";
    private Transform activeDoorApproach;
    private Door activeDoor;
    private Transform activeExitApproach;
    private Door activeExitDoor;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("BossEnemyPatrol could not find a PlayerMovement in the scene.");

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("BossEnemyPatrol requires a NavMeshAgent component!");
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.speed = patrolSpeed;

        roamCenter = transform.position;
        SetRandomTarget();
        RotateTowardsTarget();

        EnterPatrolMode();
    }

    private void Update()
    {
        if (IntroManager.IsIntroActive)
            return;

        if (isOnCooldown)
            UpdateCooldown();

        if (ignorePlayerTimer > 0f)
            ignorePlayerTimer -= Time.deltaTime;

        UpdateRoomCooldowns();

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Jumpscare:
                UpdateJumpscare();
                break;
            case EnemyState.EnterRoom:
                UpdateEnterRoom();
                break;
            case EnemyState.SearchRoom:
                UpdateSearchRoom();
                break;
            case EnemyState.ExitRoom:
                UpdateExitRoom();
                break;
        }
    }

    private void UpdateRoomCooldowns()
    {
        if (patrolRooms == null) return;

        foreach (PatrolRoom room in patrolRooms)
        {
            if (room.visitCooldownRemaining > 0f)
                room.visitCooldownRemaining -= Time.deltaTime;
        }
    }

    private void EnterPatrolMode()
    {
        currentState = EnemyState.Patrol;
    
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
        }

        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            agent.destination = targetPosition;
        }
        
        RotateTowardsTarget();
        Debug.Log($"[Boss] → Patrol | Heading to {targetPosition:F1}");
    }

    private void EnterChaseMode()
    {
        if (ignorePlayerTimer > 0f)
            return;

        if (player == null || player.IsHidden)
            return;

        currentState = EnemyState.Chase;

        savedPatternPosition = transform.position;
        savedPatternRotation = transform.rotation;
        savedTargetPosition = targetPosition;

        currentRoom = null;

        if (agent != null)
        {
            if (!agent.enabled)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }

            agent.speed = patrolChaseSpeed;
            agent.isStopped = false;
            agent.destination = player.transform.position;
        }

        chargeTimer = 0f;
        Debug.Log("[Boss] → Chase | Player detected");
    }

    private void EnterAttackMode()
    {
        if (player == null)
        {
            EnterIdleMode();
            return;
        }

        AudioManager.Instance?.PlayMonsterAttackSound("Boss");

        currentState = EnemyState.Attack;
        Debug.Log("[Boss] → Attack | Lunging at player");
        attackTimer = 0f;
        chargeTimer = 0f;

        // Disable NavMeshAgent during attack for direct movement
        if (agent != null)
            agent.enabled = false;

        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        attackTargetPosition = player.transform.position + forward * faceDistance;
        attackTargetPosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        attackTargetPosition += directionFromPlayerToEnemy * attackOffset;

        RotateTowards(attackTargetPosition);
    }

    private void PerformJumpscare()
    {
        if (player != null)
            player.StartJumpscare(transform, jumpscareDuration, jumpscareShakeIntensity);

        currentState = EnemyState.Jumpscare;
        jumpscareTimer = jumpscareDuration;
        isOnCooldown = true;
        cooldownTimer = attackCooldown;
        ignorePlayerTimer = ignorePlayerAfterAttackTime;
    }

    private void EnterIdleMode()
    {
        currentState = EnemyState.Idle;
    
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    
        idleTimer = idleTime;
        Debug.Log("[Boss] → Idle");
    }

    private void EnterRoomMode(PatrolRoom room)
    {
        currentState = EnemyState.EnterRoom;
        currentRoom = room;
        reachedDoor = false;
        
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }

        bool hasBothDoors = room.doorApproach != null && room.doorApproach2 != null;
        bool useSecond = hasBothDoors && Random.value < 0.5f;

        activeDoorApproach = useSecond ? room.doorApproach2 : room.doorApproach;
        activeDoor = useSecond ? room.door2 : room.door;

        bool exitUseSecond = hasBothDoors && Random.value < 0.5f;
        activeExitApproach = exitUseSecond ? room.doorApproach2 : room.doorApproach;
        activeExitDoor = exitUseSecond ? room.door2 : room.door;

        Vector3 dest = activeDoorApproach != null ? activeDoorApproach.position : room.interiorPoint.position;
        if (agent != null) agent.destination = dest;
        Debug.Log($"[Boss] → EnterRoom | Heading to {room.roomName} via {(useSecond ? "Door 2" : "Door 1")}");
    }

    private void EnterSearchMode()
    {
        currentState = EnemyState.SearchRoom;
        if (agent != null) agent.isStopped = true;
        searchTimer = roomSearchTime;
        Debug.Log($"[Boss] → SearchRoom | Searching {currentRoom?.roomName}");
    }

    private void EnterExitRoomMode()
    {
        currentState = EnemyState.ExitRoom;
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
        }
        reachedExitDoor = false;

        Vector3 dest = activeExitApproach != null ? activeExitApproach.position : roamCenter;
        if (agent != null) agent.destination = dest;
        Debug.Log($"[Boss] → ExitRoom | Leaving {currentRoom?.roomName}");
    }

    private void UpdateIdle()
    {   
        if (ignorePlayerTimer > 0f)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
                EnterPatrolMode();
            return;
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0 || (player != null && CanDetectPlayer(patrolDetectionRadius)))
        {
            if (player != null && CanDetectPlayer(patrolDetectionRadius))
                EnterChaseMode();
            else    
                EnterPatrolMode();
        }
    }

    private void UpdatePatrol()
    {
        if (ignorePlayerTimer > 0f)
        {
            if (agent != null && agent.enabled)
                SmoothRotateTowards(agent.destination);

            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (patrolRooms != null && patrolRooms.Length > 0 && Random.value < roomVisitChance)
                {
                    PatrolRoom room = PickWeightedRoom();
                    if (room != null)
                    {
                        EnterRoomMode(room);
                        return;
                    }
                }

                SetValidRandomTarget();
                if (agent != null) agent.destination = targetPosition;
                RotateTowardsTarget();
            }
            return;
        }

        if (player != null && CanDetectPlayer(patrolDetectionRadius))
        {
            EnterChaseMode();
            return;
        }   

        if (agent != null && agent.enabled)
            SmoothRotateTowards(agent.destination);

        if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (patrolRooms != null && patrolRooms.Length > 0 && Random.value < roomVisitChance)
            {
                PatrolRoom room = PickWeightedRoom();
                if (room != null)
                {
                    EnterRoomMode(room);
                    return;
                }
            }

            SetValidRandomTarget();
            if (agent != null) agent.destination = targetPosition;
            RotateTowardsTarget();
            Debug.Log($"[Boss] Patrol | New waypoint {targetPosition:F1}");
        }
    }

    private void UpdateChase()
    {
        if (ignorePlayerTimer > 0f)
        {
            EnterIdleMode();
            return;
        }

        if (player == null)
        {
            EnterIdleMode();
            return;
        }

        if (player.IsHidden)
        {
            EnterIdleMode();
            return;
        }

        if (!CanDetectPlayer(patrolDetectionRadius) && !CanDetectPlayer(closeDetectionRadius, true))
        {
            EnterIdleMode();
            return;
        }

        Vector3 chaseTarget = player.transform.position;
        chaseTarget.y = transform.position.y;
        if (agent != null) agent.destination = chaseTarget;

        SmoothRotateTowards(chaseTarget);

        if (Vector3.Distance(transform.position, player.transform.position) <= patrolAttackRadius && !isOnCooldown)
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer >= chargeTime)
            {
                EnterAttackMode();
                return;
            }
        }
        else
        {
            chargeTimer = 0f;
        }
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            ResetAttackLikeState();
            ReenableAgentAndIdle();
            return;
        }

        if (player.IsHidden)
        {
            ResetAttackLikeState();
            ReenableAgentAndIdle();
            return;
        }

        RotateTowards(attackTargetPosition);

        transform.position = Vector3.MoveTowards(
            transform.position,
            attackTargetPosition,
            attackSpeed * Time.deltaTime
        );

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDuration || Vector3.Distance(transform.position, attackTargetPosition) < 0.1f)
        {
            float damageRadius = patrolAttackRadius + 0.5f;

            if (Vector3.Distance(transform.position, player.transform.position) <= damageRadius)
                player.TakeDamage(player.maxHP * attackDamagePercent);

            PerformJumpscare();
        }
    }

    private void UpdateJumpscare()
    {
        if (player == null || player.IsHidden)
        {
            StopJumpscareAndReturn();
            return;
        }

        jumpscareTimer -= Time.deltaTime;

        Vector3 targetFacePosition = player.transform.position + player.transform.forward * faceDistance;
        targetFacePosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        targetFacePosition += directionFromPlayerToEnemy * attackOffset;

        Vector3 basePosition = Vector3.MoveTowards(
            transform.position,
            targetFacePosition,
            attackSpeed * Time.deltaTime
        );

        transform.position = basePosition;
        RotateTowards(player.transform.position);

        if (jumpscareTimer <= 0f)
        {
            StopJumpscareAndReturn();
        }
    }

    private void StopJumpscareAndReturn()
    {
        if (player != null)
            player.StopJumpscare();

        ReenableAgentAndIdle();
    }

    private void ReenableAgentAndIdle()
    {
        if (agent != null)
        {
            if (!agent.enabled)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        currentState = EnemyState.Idle;
        idleTimer = idleTime;

        Debug.Log($"[Boss] → ReturnToPatrol | Ignoring player for {ignorePlayerAfterAttackTime}s");
    }

    private void UpdateEnterRoom()
    {
        if (ignorePlayerTimer <= 0f && player != null && CanDetectPlayer(patrolDetectionRadius))
        {
            EnterChaseMode();
            return;
        }

        if (!reachedDoor)
        {
            Vector3 dest = activeDoorApproach != null ? activeDoorApproach.position : currentRoom.interiorPoint.position;
            SmoothRotateTowards(dest);

            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                reachedDoor = true;

                if (activeDoor != null && !activeDoor.IsOpen)
                {
                    activeDoor.ForceOpen();
                    if (agent != null) agent.isStopped = true;
                }

                if (agent != null) agent.destination = currentRoom.interiorPoint.position;
                Debug.Log($"[Boss] EnterRoom | Door reached, moving inside {currentRoom.roomName}");
            }
        }
        else
        {
            if (activeDoor != null && activeDoor.IsMoving)
            {
                if (agent != null) agent.isStopped = true;
                return;
            }

            if (agent != null) agent.isStopped = false;
            SmoothRotateTowards(currentRoom.interiorPoint.position);

            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                EnterSearchMode();
        }
    }

    private void UpdateSearchRoom()
    {
        if (ignorePlayerTimer <= 0f && player != null && CanDetectPlayer(patrolDetectionRadius))
        {
            EnterChaseMode();
            return;
        }

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
            EnterExitRoomMode();
    }

    private void UpdateExitRoom()
    {
        if (ignorePlayerTimer <= 0f && player != null && CanDetectPlayer(patrolDetectionRadius))
        {
            EnterChaseMode();
            return;
        }

        if (!reachedExitDoor)
        {
            if (agent != null) SmoothRotateTowards(agent.destination);

            if (activeExitDoor != null && !activeExitDoor.IsOpen)
            {
                float distToDoor = Vector3.Distance(transform.position, activeExitDoor.transform.position);
                if (distToDoor <= doorOpenDistance)
                {
                    reachedExitDoor = true;
                    activeExitDoor.ForceOpen();
                    if (agent != null) agent.isStopped = true;
                }
            }
            else
            {
                reachedExitDoor = true;
            }
        }
        else
        {
            if (activeExitDoor != null && activeExitDoor.IsMoving)
            {
                if (agent != null) agent.isStopped = true;
                return;
            }

            if (agent != null) agent.isStopped = false;
            if (agent != null) SmoothRotateTowards(agent.destination);

            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (currentRoom != null)
                    currentRoom.visitCooldownRemaining = roomVisitCooldown;

                currentRoom = null;
                SetValidRandomTarget();
                EnterPatrolMode();
            }
        }
    }

    private PatrolRoom GetRoomAtPosition(Vector3 pos)
    {
        if (patrolRooms == null) return null;
        foreach (PatrolRoom room in patrolRooms)
        {
            if (room.interiorPoint == null) continue;
            Bounds bounds = new Bounds(room.interiorPoint.position, room.roomSize);
            if (bounds.Contains(new Vector3(pos.x, room.interiorPoint.position.y, pos.z)))
                return room;
        }
        return null;
    }

    private PatrolRoom PickWeightedRoom()
    {
        if (patrolRooms == null || patrolRooms.Length == 0) return null;

        float totalWeight = 0f;
        foreach (PatrolRoom room in patrolRooms)
            totalWeight += room.visitCooldownRemaining <= 0f ? 1f : 0.15f;

        float rand = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (PatrolRoom room in patrolRooms)
        {
            cumulative += room.visitCooldownRemaining <= 0f ? 1f : 0.15f;
            if (rand <= cumulative)
                return room;
        }

        return patrolRooms[^1];
    }

    private bool CanDetectPlayer(float radius, bool ignoreHiding = false)
    {
        if (player == null)
            return false;

        if (ignorePlayerTimer > 0f)
            return false;

        if (!ignoreHiding && player.IsHidden)
            return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= radius;
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = 0f;
            isOnCooldown = false;
        }
    }

    private void ResetAttackLikeState()
    {
        chargeTimer = 0f;
        attackTimer = 0f;
    }

    private void SetRandomTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection.y = 0f;
        Vector3 candidate = roamCenter + randomDirection;
        candidate.y = transform.position.y;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            targetPosition = hit.position;
        else
            targetPosition = transform.position;
    }

    private void SetValidRandomTarget()
    {
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            SetRandomTarget();
            if (GetRoomAtPosition(targetPosition) == null)
                return;
        }
        targetPosition = roamCenter;
        targetPosition.y = transform.position.y;
    }

    private void RotateTowardsTarget()
    {
        RotateTowards(targetPosition);
    }

    private void RotateTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void SmoothRotateTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolDetectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, patrolAttackRadius);

        if (patrolRooms == null) return;
        foreach (PatrolRoom room in patrolRooms)
        {
            if (room.interiorPoint == null) continue;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawCube(room.interiorPoint.position, room.roomSize);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(room.interiorPoint.position, room.roomSize);
        }
    }
}
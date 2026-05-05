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

    [Header("Room Patrol")]
    public PatrolRoom[] patrolRooms;
    [Range(0f, 1f)] public float roomVisitChance = 0.4f;
    public float roomVisitCooldown = 30f;
    public float roomSearchTime = 3.5f;
    public float doorOpenDistance = 2f;

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
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.destination = targetPosition;
        RotateTowardsTarget();
        Debug.Log($"[Boss] → Patrol | Heading to {targetPosition:F1}");
    }

    private void EnterChaseMode()
    {
        currentState = EnemyState.Chase;

        savedPatternPosition = transform.position;
        savedPatternRotation = transform.rotation;
        savedTargetPosition = targetPosition;

        currentRoom = null;

        agent.speed = patrolChaseSpeed;
        agent.isStopped = false;

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

        savedPatternPosition = transform.position;
        savedPatternRotation = transform.rotation;
        savedTargetPosition = targetPosition;

        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        attackTargetPosition = player.transform.position + forward * faceDistance;
        attackTargetPosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        attackTargetPosition += directionFromPlayerToEnemy * attackOffset;

        RotateTowards(attackTargetPosition);
    }

    private void EnterIdleMode()
    {
        currentState = EnemyState.Idle;
        agent.isStopped = true;
        idleTimer = idleTime;
        Debug.Log("[Boss] → Idle");
    }

    private void EnterRoomMode(PatrolRoom room)
    {
        currentState = EnemyState.EnterRoom;
        currentRoom = room;
        reachedDoor = false;
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        bool hasBothDoors = room.doorApproach != null && room.doorApproach2 != null;
        bool useSecond = hasBothDoors && Random.value < 0.5f;

        activeDoorApproach = useSecond ? room.doorApproach2 : room.doorApproach;
        activeDoor = useSecond ? room.door2 : room.door;

        bool exitUseSecond = hasBothDoors && Random.value < 0.5f;
        activeExitApproach = exitUseSecond ? room.doorApproach2 : room.doorApproach;
        activeExitDoor = exitUseSecond ? room.door2 : room.door;

        Vector3 dest = activeDoorApproach != null ? activeDoorApproach.position : room.interiorPoint.position;
        agent.destination = dest;
        Debug.Log($"[Boss] → EnterRoom | Heading to {room.roomName} via {(useSecond ? "Door 2" : "Door 1")}");
    }

    private void EnterSearchMode()
    {
        currentState = EnemyState.SearchRoom;
        agent.isStopped = true;
        searchTimer = roomSearchTime;
        Debug.Log($"[Boss] → SearchRoom | Searching {currentRoom?.roomName}");
    }

    private void EnterExitRoomMode()
    {
        currentState = EnemyState.ExitRoom;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        reachedExitDoor = false;

        Vector3 dest = activeExitApproach != null ? activeExitApproach.position : roamCenter;
        agent.destination = dest;
        Debug.Log($"[Boss] → ExitRoom | Leaving {currentRoom?.roomName}");
    }

    private void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0 || (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true))))
        {
            if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
                EnterChaseMode();
            else
                EnterPatrolMode();
        }
    }

    private void UpdatePatrol()
    {
        if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
        {
            EnterChaseMode();
            return;
        }

        SmoothRotateTowards(agent.destination);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
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

            SetRandomTarget();
            agent.destination = targetPosition;
            RotateTowardsTarget();
            Debug.Log($"[Boss] Patrol | New waypoint {targetPosition:F1}");
        }
    }

    private void UpdateChase()
    {
        if (player == null)
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
        agent.destination = chaseTarget;

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
            EnterIdleMode();
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

            EnterIdleMode();
        }
    }

    private void UpdateEnterRoom()
    {
        if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
        {
            EnterChaseMode();
            return;
        }

        if (!reachedDoor)
        {
            Vector3 dest = activeDoorApproach != null ? activeDoorApproach.position : currentRoom.interiorPoint.position;
            SmoothRotateTowards(dest);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                reachedDoor = true;

                if (activeDoor != null && !activeDoor.IsOpen)
                {
                    activeDoor.ForceOpen();
                    agent.isStopped = true;
                }

                agent.destination = currentRoom.interiorPoint.position;
                Debug.Log($"[Boss] EnterRoom | Door reached, moving inside {currentRoom.roomName}");
            }
        }
        else
        {
            if (activeDoor != null && activeDoor.IsMoving)
            {
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;
            SmoothRotateTowards(currentRoom.interiorPoint.position);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                EnterSearchMode();
        }
    }

    private void UpdateSearchRoom()
    {
        if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
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
        if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
        {
            EnterChaseMode();
            return;
        }

        if (!reachedExitDoor)
        {
            SmoothRotateTowards(agent.destination);

            if (activeExitDoor != null && !activeExitDoor.IsOpen)
            {
                float distToDoor = Vector3.Distance(transform.position, activeExitDoor.transform.position);
                if (distToDoor <= doorOpenDistance)
                {
                    reachedExitDoor = true;
                    activeExitDoor.ForceOpen();
                    agent.isStopped = true;
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
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;
            SmoothRotateTowards(agent.destination);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (currentRoom != null)
                    currentRoom.visitCooldownRemaining = roomVisitCooldown;

                currentRoom = null;
                SetRandomTarget();
                EnterPatrolMode();
            }
        }
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
        targetPosition = roamCenter + randomDirection;
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
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoopEnemy : MonoBehaviour
{
    public enum EnemyState
    {
        WaypointPatrol,
        WaypointStop,
        RoomTeleportDown,
        RoomTeleportUp,
        RoomSearch,
        Chase,
        RoomChase,
        Attack,
        RoomAttack,
        Jumpscare,
        Confused,
        ReturnToPatrol
    }

    [Header("Waypoint System")]
    public Transform[] mainWaypoints;
    public Transform[] roomWaypoints;
    private int currentWaypointIndex = 0;
    private int waypointsVisited = 0;
    private Transform lastMainWaypoint;
    private Transform currentRoomWaypoint;

    [Header("Speeds")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 6f;
    public float roomChaseSpeed = 8f;
    public float rotationSpeed = 3f;
    public float sinkRiseSpeed = 2f;

    [Header("Detection Radii")]
    public float patrolWalkRadius = 4f;
    public float waypointStopRadius = 7f;
    public float roomSearchRadius = 14f;
    public float attackRadius = 2.5f;

    [Header("Line of Sight")]
    public LayerMask sightBlockMask = ~0;
    public float sightCheckHeight = 1f;

    [Header("Waypoint Stop Settings")]
    public float stopDuration = 3f;
    public float rotationAngle = 45f;
    public float rotationPause = 0.5f;

    [Header("Room Search")]
    public float roomSearchDuration = 5f;

    [Header("Attack Settings")]
    public float attackSpeed = 10f;
    public float attackDuration = 0.6f;
    public float attackDamagePercent = 0.3f;
    public float attackCooldown = 3f;
    public float faceDistance = 0.7f;
    public float attackOffset = 0f;

    [Header("Jumpscare Settings")]
    public float jumpscareDuration = 2f;
    public float jumpscareShakeIntensity = 1f;
    public float enemyShakeAmount = 0.1f;

    [Header("Safe Zone")]
    public LayerMask classroomMask = 0;
    public float classroomCheckRadius = 0.3f;

    [Header("Confused State")]
    public float confusedDelay = 3f; // Wait time before sinking

    [Header("Sink/Rise Settings")]
    public float sinkDepth = 1.5f;

    private PlayerMovement player;
    private EnemyState currentState;
    private EnemyState chaseStartedFromState;

    private Vector3 originalScale;
    private float sinkProgress = 0f;
    private bool isSinking = false;
    private bool isRising = false;
    private Vector3 sinkStartScale;
    private Vector3 sinkTargetScale;

    private float stopTimer = 0f;
    private float searchTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;
    private bool isRotatingRight = true;
    private float rotationPauseTimer = 0f;
    private bool isPausingRotation = false;
    private float baseYRotation;
    private float currentRotationOffset = 0f;

    private Vector3 savedPatrolPosition;
    private Quaternion savedPatrolRotation;
    private Vector3 attackTargetPosition;
    private float attackTimer = 0f;
    private float jumpscareTimer = 0f;
    private float confusedTimer = 0f;

    private float startupDelay = 5f;
    private bool hasStarted = false;

    void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("PoopEnemy could not find a PlayerMovement in the scene.");

        originalScale = transform.localScale;
        sinkStartScale = originalScale;
        sinkTargetScale = new Vector3(originalScale.x, 0.1f, originalScale.z);

        if (mainWaypoints.Length > 0)
        {
            currentWaypointIndex = 0;
        }
        else
        {
            Debug.LogError("PoopEnemy has no main waypoints assigned!");
            enabled = false;
        }
    }

    void Update()
    {
        if (IntroManager.IsIntroActive)
            return;

        if (!hasStarted)
    {
        startupDelay -= Time.deltaTime;
        if (startupDelay <= 0f)
        {
            hasStarted = true;
            EnterWaypointPatrol();
            Debug.Log("[Poop] → Startup complete, beginning patrol!");
        }
        return; // Don't do anything else during startup delay
    }

    if (isOnCooldown)
        UpdateCooldown();

    HandleSinkRise();

        switch (currentState)
        {
            case EnemyState.WaypointPatrol:
                UpdateWaypointPatrol();
                break;
            case EnemyState.WaypointStop:
                UpdateWaypointStop();
                break;
            case EnemyState.RoomTeleportDown:
                break;
            case EnemyState.RoomTeleportUp:
                break;
            case EnemyState.RoomSearch:
                UpdateRoomSearch();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.RoomChase:
                UpdateRoomChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.RoomAttack:
                UpdateRoomAttack();
                break;
            case EnemyState.Jumpscare:
                UpdateJumpscare();
                break;
            case EnemyState.Confused:
                UpdateConfused();
                break;
            case EnemyState.ReturnToPatrol:
                break;
        }
    }

    private void HandleSinkRise()
    {
        if (isSinking)
        {
            sinkProgress += Time.deltaTime * sinkRiseSpeed;
            transform.localScale = Vector3.Lerp(sinkStartScale, sinkTargetScale, sinkProgress);

            if (sinkProgress >= 1f)
            {
                isSinking = false;
                sinkProgress = 0f;
                OnSinkComplete();
            }
        }
        else if (isRising)
        {
            sinkProgress += Time.deltaTime * sinkRiseSpeed;
            transform.localScale = Vector3.Lerp(sinkStartScale, originalScale, sinkProgress);

            if (sinkProgress >= 1f)
            {
                isRising = false;
                sinkProgress = 0f;
                OnRiseComplete();
            }
        }
    }

    private void OnSinkComplete()
{
    // At this point the enemy is fully shrunk down
    
    if (currentState == EnemyState.RoomTeleportDown)
    {
        // Teleport to random room waypoint
        if (roomWaypoints.Length > 0)
        {
            currentRoomWaypoint = roomWaypoints[Random.Range(0, roomWaypoints.Length)];
            transform.position = currentRoomWaypoint.position;
        }

        // Start rising at the new location
        StartRising();
        currentState = EnemyState.RoomTeleportUp;
    }
    else if (currentState == EnemyState.ReturnToPatrol)
    {
        // Teleport to first waypoint
        currentWaypointIndex = 0;
        waypointsVisited = 0;
        
        if (mainWaypoints.Length > 0)
        {
            transform.position = mainWaypoints[0].position;
        }
        
        // Start rising at the new location
        StartRising();
        // Keep state as ReturnToPatrol for OnRiseComplete
    }
}

    private void OnRiseComplete()
{
    // At this point the enemy is fully grown at the new location
    
    if (currentState == EnemyState.RoomTeleportUp)
    {
        EnterRoomSearch();
    }
    else if (currentState == EnemyState.ReturnToPatrol)
    {
        // Finished rising at waypoint, resume patrol
        EnterWaypointPatrol();
    }
}

    private void StartSinking()
    {
        isSinking = true;
        isRising = false;
        sinkProgress = 0f;
        sinkStartScale = transform.localScale;
    }

    private void StartRising()
    {
        isRising = true;
        isSinking = false;
        sinkProgress = 0f;
        sinkStartScale = transform.localScale;
    }

    private void EnterWaypointPatrol()
    {
        currentState = EnemyState.WaypointPatrol;
        MakeSolid();
        Debug.Log($"[Poop] → WaypointPatrol | Heading to waypoint {currentWaypointIndex}");
    }

    private void EnterWaypointStop()
    {
        currentState = EnemyState.WaypointStop;
        stopTimer = stopDuration;
        isRotatingRight = true;
        isPausingRotation = false;
        rotationPauseTimer = 0f;
        baseYRotation = transform.eulerAngles.y;
        currentRotationOffset = 0f;
        Debug.Log($"[Poop] → WaypointStop | Scanning at waypoint {currentWaypointIndex}");
    }

    private void EnterRoomSearch()
    {
        currentState = EnemyState.RoomSearch;
        searchTimer = roomSearchDuration;
        baseYRotation = transform.eulerAngles.y;
        currentRotationOffset = 0f;
        isRotatingRight = true;
        MakeGhost();
        Debug.Log($"[Poop] → RoomSearch | Searching room with large detection");
    }

    private void EnterChaseMode(EnemyState startedFrom)
    {
        if (player == null || player.IsHidden)
            return;

        currentState = EnemyState.Chase;
        chaseStartedFromState = startedFrom;
        
        savedPatrolPosition = transform.position;
        savedPatrolRotation = transform.rotation;

        MakeSolid();
        attackTimer = 0f;

        AudioManager2.Instance?.PlayDetectionSound();
        
        Debug.Log("[Poop] → Chase | Player detected! Normal chase with collisions");
    }

    private void EnterRoomChaseMode()
    {
        if (player == null || player.IsHidden)
            return;

        currentState = EnemyState.RoomChase;
        
        savedPatrolPosition = transform.position;
        savedPatrolRotation = transform.rotation;

        MakeGhost();
        attackTimer = 0f;

        AudioManager2.Instance?.PlayDetectionSound();
        
        Debug.Log("[Poop] → RoomChase | Player detected in room! Ghost chase through everything!");
    }

    private void EnterAttackMode()
    {
        if (player == null)
        {
            EnterConfusedState();
            return;
        }

        currentState = EnemyState.Attack;
        attackTimer = 0f;

        AudioManager2.Instance?.PlayAttackSound();

        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        attackTargetPosition = player.transform.position + forward * faceDistance;
        attackTargetPosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        attackTargetPosition += directionFromPlayerToEnemy * attackOffset;

        RotateTowards(attackTargetPosition);
        Debug.Log("[Poop] → Attack | Normal attack with collisions");
    }

    private void EnterRoomAttackMode()
    {
        if (player == null)
        {
            EnterConfusedState();
            return;
        }

        currentState = EnemyState.RoomAttack;
        attackTimer = 0f;

        AudioManager2.Instance?.PlayAttackSound();

        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        attackTargetPosition = player.transform.position + forward * faceDistance;
        attackTargetPosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        attackTargetPosition += directionFromPlayerToEnemy * attackOffset;

        MakeGhost();
        RotateTowards(attackTargetPosition);
        Debug.Log("[Poop] → RoomAttack | Ghost attack through everything!");
    }

    private void PerformJumpscare()
    {
        if (player != null)
            player.StartJumpscare(transform, jumpscareDuration, jumpscareShakeIntensity);

        currentState = EnemyState.Jumpscare;
        jumpscareTimer = jumpscareDuration;
        isOnCooldown = true;
        cooldownTimer = attackCooldown;
        MakeGhost();
    }

    private void EnterConfusedState()
{
    if (player != null)
        player.StopJumpscare();

    currentState = EnemyState.Confused;
    confusedTimer = confusedDelay; // Wait 3 seconds before sinking
    MakeSolid();

    AudioManager2.Instance?.PlayConfusedSound();
    
    Debug.Log($"[Poop] → Confused | Player escaped! Waiting {confusedDelay} seconds before sinking...");
}

    private void ReturnToPatrol()
    {
        if (player != null)
            player.StopJumpscare();

        currentState = EnemyState.ReturnToPatrol;
        MakeSolid();
        StartSinking();
    }

    private void MakeGhost()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void MakeSolid()
    {
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * sightCheckHeight;
        Vector3 target = player.transform.position + Vector3.up * sightCheckHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, distance, sightBlockMask))
        {
            return false;
        }

        return true;
    }

    private void UpdateWaypointPatrol()
    {
        if (CanDetectPlayer(patrolWalkRadius) && HasLineOfSightToPlayer())
        {
            EnterChaseMode(EnemyState.WaypointPatrol);
            return;
        }

        Vector3 targetPosition = mainWaypoints[currentWaypointIndex].position;
        targetPosition.y = transform.position.y;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position += direction * patrolSpeed * Time.deltaTime;

        if (distance <= 0.5f)
        {
            waypointsVisited++;
            EnterWaypointStop();
        }
    }

    private void UpdateWaypointStop()
    {
        if (CanDetectPlayer(waypointStopRadius) && HasLineOfSightToPlayer())
        {
            EnterChaseMode(EnemyState.WaypointStop);
            return;
        }

        if (!isPausingRotation)
        {
            float targetOffset = isRotatingRight ? rotationAngle : -rotationAngle;
            currentRotationOffset = Mathf.Lerp(currentRotationOffset, targetOffset, rotationSpeed * Time.deltaTime);
            
            transform.rotation = Quaternion.Euler(0f, baseYRotation + currentRotationOffset, 0f);

            if (Mathf.Abs(currentRotationOffset - targetOffset) < 1f)
            {
                isPausingRotation = true;
                rotationPauseTimer = rotationPause;
                isRotatingRight = !isRotatingRight;
            }
        }
        else
        {
            rotationPauseTimer -= Time.deltaTime;
            if (rotationPauseTimer <= 0f)
            {
                isPausingRotation = false;
            }
        }

        stopTimer -= Time.deltaTime;
        if (stopTimer <= 0f)
        {
            if (waypointsVisited >= 3)
            {
                waypointsVisited = 0;
                lastMainWaypoint = mainWaypoints[currentWaypointIndex];
                savedPatrolRotation = transform.rotation;

                AudioManager2.Instance?.PlayTeleportSound();
                
                StartSinking();
                currentState = EnemyState.RoomTeleportDown;
            }
            else
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % mainWaypoints.Length;
                EnterWaypointPatrol();
            }
        }
    }

    private void UpdateRoomSearch()
    {
        if (CanDetectPlayer(roomSearchRadius))
        {
            EnterRoomChaseMode();
            return;
        }

        float rotationThisFrame = rotationSpeed * 60f * Time.deltaTime;
        currentRotationOffset += rotationThisFrame;
        transform.rotation = Quaternion.Euler(0f, baseYRotation + currentRotationOffset, 0f);

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            ReturnToPatrol();
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            EnterConfusedState();
            return;
        }

        if (player.IsHidden)
        {
            EnterConfusedState();
            return;
        }

        if (IsPlayerInsideClassroom())
        {
            EnterConfusedState();
            return;
        }

        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }

        transform.position += direction * chaseSpeed * Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRadius && !isOnCooldown)
        {
            EnterAttackMode();
        }
    }

    private void UpdateRoomChase()
    {
        if (player == null)
        {
            EnterConfusedState();
            return;
        }

        if (player.IsHidden)
        {
            EnterConfusedState();
            return;
        }

        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }

        transform.position += direction * roomChaseSpeed * Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRadius && !isOnCooldown)
        {
            EnterRoomAttackMode();
        }
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            EnterConfusedState();
            return;
        }

        if (IsPlayerInsideClassroom())
        {
            EnterConfusedState();
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
            float damageRadius = attackRadius + 0.5f;

            if (Vector3.Distance(transform.position, player.transform.position) <= damageRadius)
                player.TakeDamage(player.maxHP * attackDamagePercent);

            PerformJumpscare();
        }
    }

    private void UpdateRoomAttack()
    {
        if (player == null)
        {
            EnterConfusedState();
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
            float damageRadius = attackRadius + 0.5f;

            if (Vector3.Distance(transform.position, player.transform.position) <= damageRadius)
                player.TakeDamage(player.maxHP * attackDamagePercent);

            PerformJumpscare();
        }
    }

    private void UpdateJumpscare()
    {
        if (player == null)
        {
            EnterConfusedState();
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

        Vector3 shake = Random.insideUnitSphere * enemyShakeAmount;
        shake.y *= 0.3f;

        transform.position = basePosition + shake;
        RotateTowards(player.transform.position);

        if (jumpscareTimer <= 0f)
            EnterConfusedState(); // After jumpscare, wait then sink
    }

    private void UpdateConfused()
{
    // Just wait, don't sink yet
    confusedTimer -= Time.deltaTime;
    
    if (confusedTimer <= 0f)
    {
        // NOW start sinking after the delay
        Debug.Log("[Poop] → Confused | Done waiting, sinking now...");
        StartSinking();
        currentState = EnemyState.ReturnToPatrol; // So OnSinkComplete teleports to waypoint
    }
}

    private bool CanDetectPlayer(float radius)
    {
        if (player == null)
            return false;

        if (player.IsHidden)
            return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= radius;
    }

    private bool IsPlayerInsideClassroom()
    {
        if (player == null) return false;
        if (classroomMask.value == 0) return false;

        return Physics.CheckSphere(
            player.transform.position,
            classroomCheckRadius,
            classroomMask,
            QueryTriggerInteraction.Collide
        );
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

    private void RotateTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnDrawGizmosSelected()
    {
        if (mainWaypoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < mainWaypoints.Length; i++)
            {
                if (mainWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(mainWaypoints[i].position, 0.3f);
                    if (i < mainWaypoints.Length - 1 && mainWaypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(mainWaypoints[i].position, mainWaypoints[i + 1].position);
                    }
                }
            }
        }

        if (roomWaypoints != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < roomWaypoints.Length; i++)
            {
                if (roomWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(roomWaypoints[i].position, 0.5f);
                    Gizmos.DrawWireCube(roomWaypoints[i].position, Vector3.one * 0.5f);
                }
            }
        }

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolWalkRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, waypointStopRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, roomSearchRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (player != null)
        {
            Vector3 origin = transform.position + Vector3.up * sightCheckHeight;
            Vector3 target = player.transform.position + Vector3.up * sightCheckHeight;
            
            if (HasLineOfSightToPlayer())
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;
                
            Gizmos.DrawLine(origin, target);
        }
    }
}
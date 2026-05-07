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
        Attack,
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
    public float chaseSpeed = 8f;
    public float rotationSpeed = 3f;
    public float sinkRiseSpeed = 2f;

    [Header("Detection Radii")]
    public float patrolWalkRadius = 4f;
    public float waypointStopRadius = 7f;
    public float roomSearchRadius = 14f;
    public float attackRadius = 2.5f;

    [Header("Waypoint Stop Settings")]
    public float stopDuration = 3f;
    public float rotationAngle = 45f;
    public float rotationPause = 0.5f;

    [Header("Room Search")]
    public float roomSearchDuration = 5f;

    [Header("Attack Settings")]
    public float attackSpeed = 10f;
    public float attackDamagePercent = 0.3f;
    public float attackCooldown = 3f;
    public float faceDistance = 0.5f;

    [Header("Sink/Rise Settings")]
    public float sinkDepth = 1.5f;

    private PlayerMovement player;
    private EnemyState currentState;

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
            EnterWaypointPatrol();
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
                // Handled in HandleSinkRise
                break;
            case EnemyState.RoomTeleportUp:
                // Handled in HandleSinkRise
                break;
            case EnemyState.RoomSearch:
                UpdateRoomSearch();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.ReturnToPatrol:
                // Handled in HandleSinkRise
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
        if (currentState == EnemyState.RoomTeleportDown)
        {
            // Teleport to random room waypoint
            if (roomWaypoints.Length > 0)
            {
                currentRoomWaypoint = roomWaypoints[Random.Range(0, roomWaypoints.Length)];
                transform.position = currentRoomWaypoint.position;
            }

            // Start rising
            StartRising();
            currentState = EnemyState.RoomTeleportUp;
        }
        else if (currentState == EnemyState.ReturnToPatrol)
        {
            // Reset to first waypoint when returning from room
            currentWaypointIndex = 0;
            waypointsVisited = 0;
            
            // Teleport to first waypoint
            if (mainWaypoints.Length > 0)
            {
                transform.position = mainWaypoints[0].position;
            }
            
            // Start rising at the first waypoint
            StartRising();
        }
    }

    private void OnRiseComplete()
    {
        if (currentState == EnemyState.RoomTeleportUp)
        {
            EnterRoomSearch();
        }
        else if (currentState == EnemyState.ReturnToPatrol)
        {
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
        Debug.Log($"[Poop] → RoomSearch | Searching room with large detection");
    }

    private void EnterChaseMode()
    {
        currentState = EnemyState.Chase;
        
        // Save position for return
        savedPatrolPosition = transform.position;
        savedPatrolRotation = transform.rotation;

        // Make enemy pass through everything
        MakeGhost();
        
        Debug.Log("[Poop] → Chase | Player detected! Charging through everything!");
    }

    private void EnterAttackMode()
    {
        if (player == null)
        {
            ReturnToPatrol();
            return;
        }

        currentState = EnemyState.Attack;
        Debug.Log("[Poop] → Attack | Attacking player!");
    }

    private void ReturnToPatrol()
    {
        currentState = EnemyState.ReturnToPatrol;
        
        // Restore normal collision
        MakeSolid();
        
        // Sink and teleport back to first waypoint
        StartSinking();
    }

    private void MakeGhost()
    {
        // Disable all colliders to pass through everything
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void MakeSolid()
    {
        // Re-enable all colliders
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    private void UpdateWaypointPatrol()
    {
        // Check for player with walking detection radius
        if (CanDetectPlayer(patrolWalkRadius))
        {
            EnterChaseMode();
            return;
        }

        // Move towards current waypoint in a straight line
        Vector3 targetPosition = mainWaypoints[currentWaypointIndex].position;
        targetPosition.y = transform.position.y; // Keep same height
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        // Face the direction we're moving
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move forward (simple position change)
        transform.position += direction * patrolSpeed * Time.deltaTime;

        // Check if we reached the waypoint
        if (distance <= 0.5f)
        {
            waypointsVisited++;
            EnterWaypointStop();
        }
    }

    private void UpdateWaypointStop()
    {
        // Check for player with stop detection radius
        if (CanDetectPlayer(waypointStopRadius))
        {
            EnterChaseMode();
            return;
        }

        // Smooth rotation left and right
        if (!isPausingRotation)
        {
            float targetOffset = isRotatingRight ? rotationAngle : -rotationAngle;
            currentRotationOffset = Mathf.Lerp(currentRotationOffset, targetOffset, rotationSpeed * Time.deltaTime);
            
            transform.rotation = Quaternion.Euler(0f, baseYRotation + currentRotationOffset, 0f);

            // Check if we've rotated enough
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
            // Every 3rd waypoint, teleport to room
            if (waypointsVisited >= 3)
            {
                waypointsVisited = 0;
                lastMainWaypoint = mainWaypoints[currentWaypointIndex];
                
                // Save rotation for return
                savedPatrolRotation = transform.rotation;
                
                // Sink down for teleport
                StartSinking();
                currentState = EnemyState.RoomTeleportDown;
            }
            else
            {
                // Move to next waypoint
                currentWaypointIndex = (currentWaypointIndex + 1) % mainWaypoints.Length;
                EnterWaypointPatrol();
            }
        }
    }

    private void UpdateRoomSearch()
    {
        // Check for player with large room detection radius
        if (CanDetectPlayer(roomSearchRadius))
        {
            EnterChaseMode();
            return;
        }

        // Rotate while searching (like scanning the room)
        float rotationThisFrame = rotationSpeed * 60f * Time.deltaTime;
        currentRotationOffset += rotationThisFrame;
        transform.rotation = Quaternion.Euler(0f, baseYRotation + currentRotationOffset, 0f);

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            // No player found, return to patrol
            ReturnToPatrol();
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            ReturnToPatrol();
            return;
        }

        // Direct movement towards player - passes through EVERYTHING
        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0f;
        
        // Face the player
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
        }

        // Move through walls, objects, everything!
        transform.position += direction * chaseSpeed * Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // No charge time - instant attack when in range
        if (distanceToPlayer <= attackRadius && !isOnCooldown)
        {
            EnterAttackMode();
        }
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            ReturnToPatrol();
            return;
        }

        // Lunge at player through everything
        Vector3 attackDirection = (player.transform.position - transform.position).normalized;
        attackDirection.y = 0f;
        
        transform.rotation = Quaternion.LookRotation(attackDirection);
        transform.position += attackDirection * attackSpeed * Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= faceDistance)
        {
            // Deal damage
            player.TakeDamage(player.maxHP * attackDamagePercent);
            
            // Start cooldown
            isOnCooldown = true;
            cooldownTimer = attackCooldown;
            
            // Return to patrol after attack
            ReturnToPatrol();
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

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            cooldownTimer = 0f;
            isOnCooldown = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw waypoints
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

        // Draw room waypoints
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

        // Draw detection radii
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolWalkRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, waypointStopRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, roomSearchRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
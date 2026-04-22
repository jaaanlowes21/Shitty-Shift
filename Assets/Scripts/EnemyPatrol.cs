using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float speed = 3f;
    public Transform[] patrolWaypoints;

    [Header("Attack Settings")]
    public float detectionRadius = 10f;
    public float chargeTime = 3f;
    public float attackSpeed = 8f;
    public float attackDuration = 0.6f;
    public float attackCooldown = 2f;
    [Range(0f, 1f)]
    public float attackDamagePercent = 0.2f;
    public float jumpscareDuration = 2f;
    public float jumpscareShakeIntensity = 1f;
    public float faceDistance = 0.7f;
    public float enemyShakeAmount = 0.1f;
    public float attackOffset = 0f;

    private Vector3 targetPosition;
    private Vector3 attackTargetPosition;
    private int waypointIndex;

    private PlayerMovement player;
    private float chargeTimer;
    private bool isAttacking;
    private bool isOnCooldown;
    private bool isReturningToPatrol;
    private bool isJumpscaring;
    private float attackTimer;
    private float cooldownTimer;
    private float jumpscareTimer;

    private Vector3 savedPatrolPosition;
    private Quaternion savedPatrolRotation;
    private Vector3 savedTargetPosition;
    private int savedWaypointIndex;

    private void Start()
    {
        waypointIndex = 0;
        SetTargetPosition();
        RotateTowardsTarget();

        player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("EnemyPatrol could not find a PlayerMovement in the scene.");
    }

    private void Update()
    {
        if (IntroManager.IsIntroActive)
            return;

        if (player != null)
            UpdatePlayerDetection();

        if (isAttacking)
        {
            UpdateAttack();
            return;
        }

        if (isJumpscaring)
        {
            UpdateJumpscare();
            return;
        }

        if (isReturningToPatrol)
        {
            UpdateReturnToPatrol();
            return;
        }

        if (isOnCooldown)
            UpdateCooldown();

        UpdatePatrol();
    }

    private void UpdatePlayerDetection()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius && !isAttacking && !isOnCooldown && !isReturningToPatrol)
        {
            chargeTimer += Time.deltaTime;

            if (chargeTimer >= chargeTime)
                StartAttack();
        }
        else
        {
            chargeTimer = 0f;
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = 0f;
        chargeTimer = 0f;

        savedPatrolPosition = transform.position;
        savedPatrolRotation = transform.rotation;
        savedTargetPosition = targetPosition;
        savedWaypointIndex = waypointIndex;

        if (player != null)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            attackTargetPosition = player.transform.position + forward * faceDistance;
            attackTargetPosition.y = transform.position.y;

            // Apply attack offset towards the monster
            Vector3 directionFromPlayerToMonster = (transform.position - player.transform.position).normalized;
            attackTargetPosition += directionFromPlayerToMonster * attackOffset;

            // Lock look during attack
            player.SetLookLock(transform, attackDuration + jumpscareDuration, 0f);
        }

        RotateTowards(attackTargetPosition);
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            ResetAttackState();
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
            if (Vector3.Distance(transform.position, player.transform.position) <= detectionRadius)
                player.TakeDamage(player.maxHP * attackDamagePercent);

            PerformJumpscare();
        }
    }

    private void PerformJumpscare()
    {
        if (player != null)
            player.StartJumpscare(transform, jumpscareDuration, jumpscareShakeIntensity);

        isAttacking = false;
        isJumpscaring = true;
        jumpscareTimer = jumpscareDuration;
        isOnCooldown = true;
        cooldownTimer = attackCooldown;
    }

    private void UpdateJumpscare()
    {
        if (player == null)
        {
            StartReturnToPatrol();
            return;
        }

        jumpscareTimer -= Time.deltaTime;

        Vector3 targetFacePosition = player.transform.position + player.transform.forward * faceDistance;
        targetFacePosition.y = transform.position.y;

        // Apply attack offset towards the monster
        Vector3 directionFromPlayerToMonster = (transform.position - player.transform.position).normalized;
        targetFacePosition += directionFromPlayerToMonster * attackOffset;

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
            StartReturnToPatrol();
    }

    private void StartReturnToPatrol()
    {
        if (player != null)
            player.StopJumpscare();

        isJumpscaring = false;
        isReturningToPatrol = true;
    }

    private void UpdateReturnToPatrol()
    {
        Vector3 returnPosition = savedPatrolPosition;
        returnPosition.y = transform.position.y;

        if (Vector3.Distance(transform.position, returnPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                returnPosition,
                speed * Time.deltaTime
            );

            Vector3 direction = returnPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            transform.position = returnPosition;
            transform.rotation = savedPatrolRotation;
            isReturningToPatrol = false;
            waypointIndex = savedWaypointIndex;
            targetPosition = savedTargetPosition;
        }
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
            isOnCooldown = false;
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        isOnCooldown = false;
        isReturningToPatrol = false;
        chargeTimer = 0f;
        attackTimer = 0f;
        cooldownTimer = 0f;
    }

    private void UpdatePatrol()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            waypointIndex = (waypointIndex + 1) % patrolWaypoints.Length;
            SetTargetPosition();
            RotateTowardsTarget();
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    private void SetTargetPosition()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;
        Transform point = patrolWaypoints[waypointIndex];
        targetPosition = point != null ? point.position : transform.position;
        targetPosition.y = transform.position.y;
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (patrolWaypoints == null || patrolWaypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            if (patrolWaypoints[i] == null) continue;
            Gizmos.DrawSphere(patrolWaypoints[i].position, 0.2f);
            Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[(i + 1) % patrolWaypoints.Length].position);
        }
    }
}

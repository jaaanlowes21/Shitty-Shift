using System.Collections;
using UnityEngine;

public class BossEnemyPatrol : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack
    }

    [Header("Patrol Area")]
    public float leftX = -180f;
    public float rightX = -65f;
    public bool startMovingRight = true;

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

    private PlayerMovement player;

    private EnemyState currentState;

    private Vector3 targetPosition;
    private Vector3 attackTargetPosition;

    private bool movingRight;
    private bool isOnCooldown;

    private float chargeTimer;
    private float attackTimer;
    private float cooldownTimer;
    private float idleTimer;

    private Vector3 savedPatternPosition;
    private Quaternion savedPatternRotation;
    private Vector3 savedTargetPosition;
    private bool savedMovingRight;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("BossEnemyPatrol could not find a PlayerMovement in the scene.");

        movingRight = startMovingRight;
        SetTargetPosition();
        RotateTowardsTarget();

        EnterPatrolMode();
    }

    private void Update()
    {
        if (IntroManager.IsIntroActive)
            return;

        if (isOnCooldown)
            UpdateCooldown();

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
        }
    }

    private void EnterPatrolMode()
    {
        currentState = EnemyState.Patrol;
        SetTargetPosition();
        RotateTowardsTarget();
    }

    private void EnterChaseMode()
    {
        currentState = EnemyState.Chase;

        savedPatternPosition = transform.position;
        savedPatternRotation = transform.rotation;
        savedTargetPosition = targetPosition;
        savedMovingRight = movingRight;

        chargeTimer = 0f;
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
        attackTimer = 0f;
        chargeTimer = 0f;

        savedPatternPosition = transform.position;
        savedPatternRotation = transform.rotation;
        savedTargetPosition = targetPosition;
        savedMovingRight = movingRight;

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
        transform.position = savedPatternPosition;
        transform.rotation = savedPatternRotation;
        idleTimer = idleTime;
    }

    private void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0 || (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true))))
        {
            if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
            {
                EnterChaseMode();
            }
            else
            {
                EnterPatrolMode();
            }
        }
    }

    private void UpdatePatrol()
    {
        if (player != null && (CanDetectPlayer(patrolDetectionRadius) || CanDetectPlayer(closeDetectionRadius, true)))
        {
            EnterChaseMode();
            return;
        }

        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, patrolSpeed * Time.deltaTime);
        transform.position = next;

        SmoothRotateTowards(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
        {
            transform.position = targetPosition;
            movingRight = !movingRight;
            SetTargetPosition();
            RotateTowardsTarget();
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

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        Vector3 chaseTarget = player.transform.position;
        chaseTarget.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            chaseTarget,
            patrolChaseSpeed * Time.deltaTime
        );

        SmoothRotateTowards(chaseTarget);

        if (distanceToPlayer <= patrolAttackRadius && !isOnCooldown)
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

    private void SetTargetPosition()
    {
        targetPosition = new Vector3(
            movingRight ? rightX : leftX,
            transform.position.y,
            transform.position.z
        );
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
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float speed = 3f;
    public float leftX = -180f;
    public float rightX = -65f;
    public bool startMovingRight = true;

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

    private Vector3 targetPosition;
    private bool movingRight;

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
    private bool savedMovingRight;

    private void Start()
    {
        movingRight = startMovingRight;
        SetTargetPosition();
        RotateTowardsTarget();

        player = FindObjectOfType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("EnemyPatrol could not find a PlayerMovement in the scene.");
    }

    private void Update()
    {
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
        savedMovingRight = movingRight;

        RotateTowards(player.transform.position);
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            ResetAttackState();
            return;
        }

        Vector3 playerPosition = player.transform.position;
        playerPosition.y = transform.position.y;

        RotateTowards(playerPosition);

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerPosition,
            attackSpeed * Time.deltaTime
        );

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDuration || Vector3.Distance(transform.position, playerPosition) < 1f)
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
            targetPosition = savedTargetPosition;
            movingRight = savedMovingRight;
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
        if (Mathf.Approximately(transform.position.x, targetPosition.x))
        {
            movingRight = !movingRight;
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
        targetPosition = new Vector3(
            movingRight ? rightX : leftX,
            transform.position.y,
            transform.position.z
        );
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
    }
}

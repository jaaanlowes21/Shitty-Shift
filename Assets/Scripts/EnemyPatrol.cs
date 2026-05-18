using System.Collections;
using UnityEngine;

public class MopPatrol : MonoBehaviour
{
    public enum EnemyState
    {
        Lookout,
        Patrol,
        Chase,
        Attack,
        Jumpscare,
        ReturnToPattern
    }

    [Header("Patrol Area")]
    public float leftX = -180f;
    public float rightX = -65f;
    public bool startMovingRight = true;

    [Header("Speeds")]
    public float patrolSpeed = 2.5f;
    public float lookoutChaseSpeed = 6f;
    public float patrolChaseSpeed = 4.5f;
    public float returnSpeed = 3f;
    public float rotationSpeed = 8f;

    [Header("Lookout Detection")]
    public float lookoutDetectionRadius = 14f;
    public float lookoutAttackRadius = 2f;

    [Header("Patrol Detection")]
    public float patrolDetectionRadius = 9f;
    public float patrolAttackRadius = 2.2f;

    [Header("Intro Lock")]
    public bool introLookoutLocked = true;

    [Header("Lookout / Patrol Pattern")]
    public float lookoutMinDuration = 2f;
    public float lookoutMaxDuration = 5f;
    public float patrolEndPauseMin = 0.4f;
    public float patrolEndPauseMax = 1.2f;
    public int patrolEndsBeforeLookoutMin = 2;
    public int patrolEndsBeforeLookoutMax = 5;

    [Header("Classroom / Safe Zone")]
    public LayerMask classroomMask = 0;
    public float classroomCheckRadius = 0.3f;

    [Header("Attack Settings")]
    public float chargeTime = 1.2f;
    public float attackSpeed = 8f;
    public float attackDuration = 0.6f;
    public float attackCooldown = 2f;
    [Range(0f, 1f)] public float attackDamagePercent = 0.2f;
    public float jumpscareDuration = 2f;
    public float jumpscareShakeIntensity = 1f;
    public float faceDistance = 0.7f;
    public float enemyShakeAmount = 0.1f;
    public float attackOffset = 0f;

    [Header("After Attack")]
    public float ignorePlayerAfterAttackTime = 5f;

    [Header("Jaw Animation")]
    public Transform jawPivot;
    public float idleJawSpeed = 2f;      // Speed of jaw bobbing when idle/patrol
    public float idleJawAngle = 15f;      // Max X angle when idle/patrol
    public float chaseJawSpeed = 8f;      // Faster jaw bobbing when chasing
    public float attackJawSpeed = 20f;    // Very fast Z rotation during attack/jumpscare
    public float attackJawAngle = 15f;    // Max Z angle during attack/jumpscare

    private PlayerMovement player;

    private EnemyState currentState;
    private EnemyState chaseStartedFromState;

    private Vector3 targetPosition;
    private Vector3 attackTargetPosition;

    private bool movingRight;
    private bool isOnCooldown;
    private bool hasSeenPlayerOnce;
    private bool hasShownDetectionHint;
    private bool returningAfterAttack;

    private float chargeTimer;
    private float attackTimer;
    private float cooldownTimer;
    private float jumpscareTimer;
    private float lookoutTimer;
    private float ignorePlayerTimer;

    private int patrolEndCount = 0;
    private int patrolEndsBeforeLookout = 3;

    private Vector3 savedPatternPosition;
    private Quaternion savedPatternRotation;
    private Vector3 savedTargetPosition;
    private bool savedMovingRight;

    private Coroutine patrolPauseCoroutine;

    // Jaw animation variables
    private float jawAnimationTimer = 0f;
    private bool jawResetNeeded = false;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogWarning("MopPatrol could not find a PlayerMovement in the scene.");

        movingRight = startMovingRight;
        SetTargetPosition();
        RotateTowardsTarget();

        patrolEndsBeforeLookout = Random.Range(patrolEndsBeforeLookoutMin, patrolEndsBeforeLookoutMax + 1);

        hasSeenPlayerOnce = false;
        hasShownDetectionHint = false;
        returningAfterAttack = false;

        // Initialize jaw to 0
        if (jawPivot != null)
            jawPivot.localRotation = Quaternion.identity;

        EnterLookoutMode();
    }

    private void Update()
    {
        if (IntroManager.IsIntroActive)
            return;

        if (isOnCooldown)
            UpdateCooldown();

        if (ignorePlayerTimer > 0f)
            ignorePlayerTimer -= Time.deltaTime;

        // Update jaw animation
        UpdateJawAnimation();

        switch (currentState)
        {
            case EnemyState.Lookout:
                UpdateLookout();
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

            case EnemyState.ReturnToPattern:
                UpdateReturnToPattern();
                break;
        }
    }

    private void UpdateJawAnimation()
    {
        if (jawPivot == null) return;

        jawAnimationTimer += Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Lookout:
            case EnemyState.Patrol:
            case EnemyState.ReturnToPattern:
                // Idle/Patrol: Rotate X axis from 0 to 15 and back, smoothly
                if (jawResetNeeded)
                {
                    // Smoothly return to idle animation
                    Quaternion currentRot = jawPivot.localRotation;
                    float targetX = Mathf.Sin(jawAnimationTimer * idleJawSpeed) * idleJawAngle;
                    jawPivot.localRotation = Quaternion.Lerp(currentRot, Quaternion.Euler(targetX, 0f, 0f), Time.deltaTime * 5f);
                    
                    if (Quaternion.Angle(currentRot, Quaternion.Euler(targetX, 0f, 0f)) < 1f)
                        jawResetNeeded = false;
                }
                else
                {
                    float xAngle = Mathf.Sin(jawAnimationTimer * idleJawSpeed) * idleJawAngle;
                    jawPivot.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
                }
                break;

            case EnemyState.Chase:
                // Chase: Faster X axis bobbing
                if (jawResetNeeded)
                {
                    Quaternion currentRot = jawPivot.localRotation;
                    float targetX = Mathf.Sin(jawAnimationTimer * chaseJawSpeed) * idleJawAngle;
                    jawPivot.localRotation = Quaternion.Lerp(currentRot, Quaternion.Euler(targetX, 0f, 0f), Time.deltaTime * 10f);
                    
                    if (Quaternion.Angle(currentRot, Quaternion.Euler(targetX, 0f, 0f)) < 1f)
                        jawResetNeeded = false;
                }
                else
                {
                    float xAngle = Mathf.Sin(jawAnimationTimer * chaseJawSpeed) * idleJawAngle;
                    jawPivot.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
                }
                break;

            case EnemyState.Attack:
            case EnemyState.Jumpscare:
                // Attack/Jumpscare: X back to 0, Z goes from -15 to 15 very fast
                float zAngle = Mathf.Sin(jawAnimationTimer * attackJawSpeed) * attackJawAngle;
                jawPivot.localRotation = Quaternion.Euler(0f, 0f, zAngle);
                jawResetNeeded = true; // Need to reset when going back to idle
                break;
        }
    }

    private void ResetJawAnimation()
    {
        jawAnimationTimer = 0f;
        if (jawPivot != null)
            jawPivot.localRotation = Quaternion.identity;
        jawResetNeeded = false;
    }

    private void EnterLookoutMode()
    {
        currentState = EnemyState.Lookout;
        lookoutTimer = Random.Range(lookoutMinDuration, lookoutMaxDuration);
        chargeTimer = 0f;

        AudioManager.Instance?.PlayLookoutSound();

        if (patrolPauseCoroutine != null)
        {
            StopCoroutine(patrolPauseCoroutine);
            patrolPauseCoroutine = null;
        }
    }

    private void EnterPatrolMode()
    {
        if (ignorePlayerTimer > 0f)
        {
            EnterLookoutMode();
            return;
        }

        currentState = EnemyState.Patrol;
        chargeTimer = 0f;

        if (patrolPauseCoroutine != null)
        {
            StopCoroutine(patrolPauseCoroutine);
            patrolPauseCoroutine = null;
        }

        SetTargetPosition();
        RotateTowardsTarget();
    }

    private void EnterChaseMode(EnemyState startedFrom)
    {
        if (ignorePlayerTimer > 0f)
            return;

        if (player == null || player.IsHidden)
            return;

        currentState = EnemyState.Chase;
        chaseStartedFromState = startedFrom;

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
            StartReturnToPattern();
            return;
        }

        AudioManager.Instance?.PlayMonsterAttackSound("Mop");

        currentState = EnemyState.Attack;
        attackTimer = 0f;
        chargeTimer = 0f;

        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        attackTargetPosition = player.transform.position + forward * faceDistance;
        attackTargetPosition.y = transform.position.y;

        Vector3 directionFromPlayerToEnemy = (transform.position - player.transform.position).normalized;
        attackTargetPosition += directionFromPlayerToEnemy * attackOffset;

        RotateTowards(attackTargetPosition);
    }

    private void StartReturnToPattern()
    {
        if (player != null)
            player.StopJumpscare();

        currentState = EnemyState.ReturnToPattern;
        chargeTimer = 0f;
        
        // Reset jaw when returning
        ResetJawAnimation();
    }

    private void UpdateLookout()
    {
        if (player == null)
            return;

        if (ignorePlayerTimer > 0f)
            return;

        if (player.IsHidden)
            return;

        if (!IsPlayerInsideClassroom())
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= lookoutDetectionRadius)
            {
                hasSeenPlayerOnce = true;
                introLookoutLocked = false;

                if (!hasShownDetectionHint)
                {
                    hasShownDetectionHint = true;
                    HintManager.Instance?.ShowHint("Is something there?");
                }

                EnterChaseMode(EnemyState.Lookout);
                return;
            }
        }

        if (introLookoutLocked && !hasSeenPlayerOnce)
            return;

        lookoutTimer -= Time.deltaTime;

        if (lookoutTimer <= 0f)
        {
            patrolEndCount = 0;
            patrolEndsBeforeLookout = Random.Range(patrolEndsBeforeLookoutMin, patrolEndsBeforeLookoutMax + 1);
            EnterPatrolMode();
        }
    }

    private void UpdatePatrol()
    {
        if (ignorePlayerTimer > 0f)
        {
            EnterLookoutMode();
            return;
        }

        if (player != null && !player.IsHidden && CanSeePlayer(patrolDetectionRadius))
        {
            EnterChaseMode(EnemyState.Patrol);
            return;
        }

        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, patrolSpeed * Time.deltaTime);
        transform.position = next;

        SmoothRotateTowards(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.05f)
        {
            transform.position = targetPosition;

            if (patrolPauseCoroutine == null)
                patrolPauseCoroutine = StartCoroutine(PatrolTurnRoutine());
        }
    }

    private IEnumerator PatrolTurnRoutine()
    {
        float waitTime = Random.Range(patrolEndPauseMin, patrolEndPauseMax);
        yield return new WaitForSeconds(waitTime);

        patrolEndCount++;

        if (patrolEndCount >= patrolEndsBeforeLookout)
        {
            patrolEndCount = 0;
            patrolEndsBeforeLookout = Random.Range(patrolEndsBeforeLookoutMin, patrolEndsBeforeLookoutMax + 1);
            patrolPauseCoroutine = null;
            EnterLookoutMode();
            yield break;
        }

        movingRight = !movingRight;
        SetTargetPosition();
        RotateTowardsTarget();

        patrolPauseCoroutine = null;
    }

    private void UpdateChase()
    {
        if (ignorePlayerTimer > 0f)
        {
            StartReturnToPattern();
            return;
        }

        if (player == null)
        {
            StartReturnToPattern();
            return;
        }

        if (player.IsHidden)
        {
            StartReturnToPattern();
            return;
        }

        if (IsPlayerInsideClassroom())
        {
            StartReturnToPattern();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        float chaseSpeed;
        float activeAttackRadius;

        if (chaseStartedFromState == EnemyState.Lookout)
        {
            chaseSpeed = lookoutChaseSpeed;
            activeAttackRadius = lookoutAttackRadius;
        }
        else
        {
            chaseSpeed = patrolChaseSpeed;
            activeAttackRadius = patrolAttackRadius;
        }

        Vector3 chaseTarget = player.transform.position;
        chaseTarget.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            chaseTarget,
            chaseSpeed * Time.deltaTime
        );

        SmoothRotateTowards(chaseTarget);

        if (distanceToPlayer <= activeAttackRadius && !isOnCooldown)
        {
            if (chaseStartedFromState == EnemyState.Lookout)
            {
                EnterAttackMode();
                return;
            }
            else
            {
                chargeTimer += Time.deltaTime;

                if (chargeTimer >= chargeTime)
                {
                    EnterAttackMode();
                    return;
                }
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
            StartReturnToPattern();
            return;
        }

        if (IsPlayerInsideClassroom())
        {
            ResetAttackLikeState();
            StartReturnToPattern();
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
            float damageRadius = (chaseStartedFromState == EnemyState.Lookout)
                ? lookoutAttackRadius + 0.5f
                : patrolAttackRadius + 0.5f;

            if (Vector3.Distance(transform.position, player.transform.position) <= damageRadius)
                player.TakeDamage(player.maxHP * attackDamagePercent);

            PerformJumpscare();
        }
    }

    private void PerformJumpscare()
    {
        if (player != null)
            player.StartJumpscare(transform, jumpscareDuration, jumpscareShakeIntensity);

        currentState = EnemyState.Jumpscare;
        jumpscareTimer = jumpscareDuration;
        isOnCooldown = true;
        cooldownTimer = attackCooldown;
        returningAfterAttack = true;
    }

    private void UpdateJumpscare()
    {
        if (player == null)
        {
            StartReturnToPattern();
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
            StartReturnToPattern();
    }

    private void UpdateReturnToPattern()
    {
        Vector3 returnPosition = savedPatternPosition;
        returnPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            returnPosition,
            returnSpeed * Time.deltaTime
        );

        SmoothRotateTowards(returnPosition);

        if (Vector3.Distance(transform.position, returnPosition) <= 0.05f)
        {
            transform.position = returnPosition;
            transform.rotation = savedPatternRotation;
            targetPosition = savedTargetPosition;
            movingRight = savedMovingRight;
            chargeTimer = 0f;

            if (returningAfterAttack)
            {
                returningAfterAttack = false;
                ignorePlayerTimer = ignorePlayerAfterAttackTime;
                EnterLookoutMode();
                return;
            }

            if (chaseStartedFromState == EnemyState.Lookout)
                EnterLookoutMode();
            else
                EnterPatrolMode();
        }
    }

    private bool CanSeePlayer(float radius)
    {
        if (ignorePlayerTimer > 0f) return false;
        if (player == null) return false;
        if (player.IsHidden) return false; 
        if (IsPlayerInsideClassroom()) return false;

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

    private void ResetAttackLikeState()
    {
        chargeTimer = 0f;
        attackTimer = 0f;
        jumpscareTimer = 0f;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lookoutDetectionRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, lookoutAttackRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolDetectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, patrolAttackRadius);
    }
}
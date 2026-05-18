using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float crouchSpeed = 2.5f;
    public float acceleration = 10f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    public float staminaRegenDelay = 1.5f;
    public float hideStaminaRegenMultiplier = 1.5f;
    public Image staminaFillImage;
    public Image hpFillImage;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float groundedGravity = -2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.1f;
    public Transform playerBody;
    public Transform cameraPivot;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    [Header("Audio")]
    public AudioClip[] walkSounds;
    public AudioClip runSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Camera")]
    public GameObject fpsCamera;
    public GameObject tpsCamera;

    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask interactMask = ~0;
    public Transform interactSource;
    public LayerMask visibilityBlockMask = ~0;

    [Header("HP")]
    public float maxHP = 100f;
    [Header("HP Stages UI")]
    public GameObject[] hpStageIndicators = new GameObject[5];

    [Header("UI")]
    public TextMeshProUGUI interactionText;

    [Header("Low Stamina Vignette")]
    public Image staminaVignette;
    [Range(0f, 1f)] public float vignetteStartNormalized = 0.4f;
    [Range(0f, 1f)] public float vignetteMaxAlpha = 0.65f;
    public float vignetteFadeSpeed = 6f;

    private Outline currentOutline;

    [Header("Animation")]
    public Animator animator;
    private bool isDancing = false;

    private CharacterController controller;
    private AudioSource audioSource;
    private float footstepTimer = 0f;
    private Vector3 velocity;
    private Vector3 currentMove;

    private bool isGrounded;
    private bool isCrouching;
    private bool isRunning;
    private bool isFPS = true;
    private bool isHidden = false;
    private bool isReading = false;
    private bool isLookLocked = false;
    private float lookLockTimer = 0f;
    private float lookLockDuration = 0f;
    private Transform lookLockTarget;
    private float lookShakeIntensity = 0f;

    private bool savedIsFPSBeforeJumpscare;
    private bool restoreCameraAfterJumpscare = false;
    private bool savedIsFPSBeforeHide;
    private bool restoreCameraAfterHide = false;

    private float xRotation = 0f;

    private float currentStamina;
    private float staminaRegenTimer = 0f;

    private float currentHP;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private IInteractable currentInteractable;
    private HideableObject hiddenInside;

    private float hiddenYOffset = 0f;
    private float hiddenBaseYaw = 0f;

    public bool IsHidden => isHidden;
    public float StaminaNormalized => currentStamina / maxStamina;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        audioSource = GetComponent<AudioSource>();
        currentStamina = maxStamina;
        currentHP = maxHP;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();
        inputActions.Player.ToggleView.performed += ctx => TryToggleCamera();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        
            // Store the initial local position and force it to zero
            Vector3 pos = animator.transform.localPosition;
            pos.y = 0f;
            animator.transform.localPosition = pos;
        
            // If the animator is not a child, make sure it stays at the player's position
            if (animator.transform != transform)
            {
                animator.transform.SetParent(transform);
                animator.transform.localPosition = Vector3.zero;
            }
        }

        UpdateHPStageIndicators();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (fpsCamera != null) fpsCamera.SetActive(true);
        if (tpsCamera != null) tpsCamera.SetActive(false);

        controller.height = standingHeight;
        controller.center = Vector3.zero;

        if (interactSource == null)
            interactSource = cameraPivot != null ? cameraPivot : transform;

        if (staminaVignette != null)
        {
            Color c = staminaVignette.color;
            c.a = 0f;
            staminaVignette.color = c;
        }

        if (visibilityBlockMask.value == ~0) 
        {
            visibilityBlockMask = ~LayerMask.GetMask("Interactable");
        }
    }

    void Update()
    {
        // Check for both intro managers
        if (IntroManager.IsIntroActive || SecondFloorManager.IsSecondFloorIntroActive)
        {
            currentMove = Vector3.zero;
            moveInput = Vector2.zero;
            isRunning = false;
            HandleGravity();
            UpdateInteractionUI();
            UpdateStatsUI();
            UpdateStaminaVignette();
            return;
        }

        // Check if game is paused (InGameMenu)
        if (Time.timeScale == 0f)
        {
            return; // Don't process any input when paused
        }

        HandleInteraction();
        bool prevGrounded = isGrounded;
        HandleGroundCheck();
        HandleLanding(prevGrounded);
        HandleMouseLook();
        HandleStamina();

        if (!isHidden && !isReading && !isLookLocked)
        {
            HandleMovement();
            HandleCrouch();
        }
        else
        {
            currentMove = Vector3.zero;
            moveInput = Vector2.zero;
            isRunning = false;
        }

        HandleFootstepAudio();
        DetectInteractable();
        HandleGravity();
        UpdateInteractionUI();
        UpdateStatsUI();
        UpdateStaminaVignette();
        HandleDance();
        UpdateAnimations();
    }

    void HandleInteraction()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (isHidden && hiddenInside != null)
            {
                hiddenInside.Interact(this);
                return;
            }

            if (currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }
    }

    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (!isGrounded && groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    void HandleStamina()
    {
        bool wantsRun = false;

        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            wantsRun = true;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        isRunning = wantsRun && isMoving && isGrounded && !isCrouching && !isHidden && !isReading && currentStamina > 0f;

        if (isRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                float regenRate = staminaRegenRate;

                if (isHidden)
                    regenRate *= hideStaminaRegenMultiplier;

                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }
    }

    void HandleMovement()
    {
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 targetMove = (playerBody.right * moveInput.x + playerBody.forward * moveInput.y) * speed;
        currentMove = Vector3.Lerp(currentMove, targetMove, Time.deltaTime * acceleration);
    }

    void HandleMouseLook()
    {
        // Don't allow mouse look when reading, intro active, or game paused
        if (isReading || IntroManager.IsIntroActive || SecondFloorManager.IsSecondFloorIntroActive || Time.timeScale == 0f)
        {
            // Show cursor when paused
            if (Time.timeScale == 0f)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        // Ensure cursor is locked during gameplay
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (isLookLocked)
        {
            UpdateLookLock();
            return;
        }

        if (isHidden)
        {
            float mouseX = lookInput.x * mouseSensitivity;
            hiddenYOffset += mouseX;
            hiddenYOffset = Mathf.Clamp(hiddenYOffset, -15f, 15f);

            if (playerBody != null)
                playerBody.rotation = Quaternion.Euler(0f, hiddenBaseYaw + hiddenYOffset, 0f);

            return;
        }

        float mx = lookInput.x * mouseSensitivity;
        float my = lookInput.y * mouseSensitivity;

        xRotation -= my;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mx);
    }

    void Jump()
    {
        if (isHidden || isReading || isDancing)
            return;

        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
                animator.SetBool("IsJumping", true);

            PlaySound(jumpSound);
        }
    }
    void HandleLanding(bool prevGrounded)
    {
        if (!prevGrounded && isGrounded)
        {
            PlaySound(landSound);

            if (animator != null)
                animator.SetBool("IsJumping", false);
        }
    }

    void HandleFootstepAudio()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (!isGrounded || !isMoving || isHidden || isReading || isCrouching)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            if (isRunning)
                PlaySound(runSound);
            else
                PlayRandomWalkSound();

            footstepTimer = isRunning ? runStepInterval : walkStepInterval;
        }
    }

    void PlayRandomWalkSound()
    {
        if (walkSounds == null || walkSounds.Length == 0) return;
        PlaySound(walkSounds[Random.Range(0, walkSounds.Length)]);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    void HandleCrouch()
{
    bool crouchPressed = false;

    if (inputActions != null)
        crouchPressed = inputActions.Player.Crouch.IsPressed();

    if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
        crouchPressed = true;

    bool isMoving = moveInput.sqrMagnitude > 0.01f;

    // Can only ENTER crouch while idle
    if (!isMoving)
    {
        isCrouching = crouchPressed &&
                      isGrounded &&
                      !isHidden &&
                      !isReading;
    }

    // Exit crouch immediately if key released
    if (!crouchPressed)
    {
        isCrouching = false;
    }

    float targetHeight = isCrouching ? crouchHeight : standingHeight;

    controller.height = Mathf.Lerp(
        controller.height,
        targetHeight,
        Time.deltaTime * crouchTransitionSpeed
    );

    Vector3 center = controller.center;
    center.y = 0f;
    controller.center = center;
}

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0f)
            velocity.y = groundedGravity;

        if (!isGrounded)
            velocity.y += gravity * Time.deltaTime;

        Vector3 move = currentMove + new Vector3(0f, velocity.y, 0f);
        controller.Move(move * Time.deltaTime);
    }

    void TryToggleCamera()
    {
        if (isHidden || isReading || IntroManager.IsIntroActive || isLookLocked) return;
        if (fpsCamera == null || tpsCamera == null) return;

        isFPS = !isFPS;
        fpsCamera.SetActive(isFPS);
        tpsCamera.SetActive(!isFPS);
    }

    void DetectInteractable()
{
    currentInteractable = null;

    if (isHidden)
        return;

    // Just use the direct raycast approach for interaction
    IInteractable raycastHit = GetInteractableFromRaycast();
    if (raycastHit != null)
    {
        currentInteractable = raycastHit;
        return;
    }

    // Fallback to sphere overlap for nearby objects
    Collider[] hits = Physics.OverlapSphere(transform.position, interactDistance, interactMask);

    float closestDistance = Mathf.Infinity;
    IInteractable closestInteractable = null;

    foreach (Collider col in hits)
    {
        MonoBehaviour[] behaviours = col.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                float dist = Vector3.Distance(transform.position, behaviour.transform.position);
                
                if (dist <= interactable.interactionRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }
    }

    currentInteractable = closestInteractable;
}

private IInteractable GetInteractableFromRaycast()
{
    Transform source = interactSource != null ? interactSource : transform;
    
    RaycastHit[] hits = Physics.RaycastAll(source.position, source.forward, interactDistance, interactMask);
    
    // Sort by distance
    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
    
    foreach (RaycastHit hit in hits)
    {
        MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
        
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                if (hit.distance <= interactable.interactionRadius)
                {
                    return interactable;
                }
            }
        }
        
        // If we hit something non-interactable (wall), stop
        if (hit.collider.GetComponentInParent<IInteractable>() == null)
        {
            return null;
        }
    }
    
    return null;
}

private void EnableOutline(IInteractable interactable)
{
    MonoBehaviour mb = interactable as MonoBehaviour;
    if (mb != null)
    {
        Outline outline = mb.GetComponentInChildren<Outline>(true);
        if (outline == null)
            outline = mb.GetComponent<Outline>();
        
        if (outline != null)
        {
            outline.enabled = true;
            currentOutline = outline;
        }
    }
}



// Add this new method to check line of sight to an object
private bool HasLineOfSightToObject(Vector3 targetPosition)
{
    Transform source = interactSource != null ? interactSource : transform;
    Vector3 direction = targetPosition - source.position;
    float distance = direction.magnitude;
    
    // Raycast to check if something is blocking the view
    if (Physics.Raycast(source.position, direction.normalized, distance, interactMask))
    {
        // The raycast hit something - check if it's the same object we're looking at
        // We need to see if the hit object is related to our interactable
        return false; // Something is blocking
    }
    
    return true; // Clear line of sight
}

    public void EnterHide(HideableObject hideable)
    {
        if (hideable == null || hideable.hidePoint == null || hideable.exitPoint == null)
        {
            Debug.LogWarning("HideableObject is missing hidePoint or exitPoint.");
            return;
        }

        savedIsFPSBeforeHide = isFPS;
        restoreCameraAfterHide = true;

        isHidden = true;
        hiddenInside = hideable;

        velocity = Vector3.zero;
        currentMove = Vector3.zero;
        moveInput = Vector2.zero;

        controller.enabled = false;
        transform.position = hideable.hidePoint.position;

        Vector3 euler = hideable.hidePoint.rotation.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        transform.rotation = Quaternion.Euler(0f, euler.y + 180f, 0f);

        controller.enabled = true;

        hiddenBaseYaw = transform.eulerAngles.y;
        hiddenYOffset = 0f;

        xRotation = 0f;
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.identity;

        if (!isFPS)
        {
            isFPS = true;
            if (fpsCamera != null) fpsCamera.SetActive(true);
            if (tpsCamera != null) tpsCamera.SetActive(false);
        }
    }

    public void ExitHide()
    {
        if (hiddenInside == null || hiddenInside.exitPoint == null) return;

        isHidden = false;

        velocity = Vector3.zero;
        currentMove = Vector3.zero;
        moveInput = Vector2.zero;

        controller.enabled = false;
        transform.position = hiddenInside.exitPoint.position;
        transform.rotation = hiddenInside.exitPoint.rotation;
        controller.enabled = true;

        hiddenInside = null;

        if (restoreCameraAfterHide && fpsCamera != null && tpsCamera != null)
        {
            isFPS = savedIsFPSBeforeHide;
            fpsCamera.SetActive(isFPS);
            tpsCamera.SetActive(!isFPS);
            restoreCameraAfterHide = false;
        }
    }

    public HideableObject GetHiddenInside()
    {
        return hiddenInside;
    }

    public void SetReadingState(bool reading)
    {
        isReading = reading;

        currentMove = Vector3.zero;
        moveInput = Vector2.zero;
        isRunning = false;

        if (reading)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void StartJumpscare(Transform target, float duration, float shakeIntensity)
    {
        if (target == null)
            return;

        savedIsFPSBeforeJumpscare = isFPS;
        restoreCameraAfterJumpscare = true;

        if (!isFPS)
        {
            isFPS = true;
            if (fpsCamera != null) fpsCamera.SetActive(true);
            if (tpsCamera != null) tpsCamera.SetActive(false);
        }

        isLookLocked = true;
        lookLockTimer = 0f;
        lookLockDuration = duration;
        lookLockTarget = target;
        lookShakeIntensity = shakeIntensity;

        currentMove = Vector3.zero;
        moveInput = Vector2.zero;
        isRunning = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetLookLock(Transform target, float duration, float intensity = 0f)
    {
        isLookLocked = true;
        lookLockTimer = 0f;
        lookLockDuration = duration;
        lookLockTarget = target;
        lookShakeIntensity = intensity;
    }

    public void StopJumpscare()
    {
        isLookLocked = false;
        lookLockTimer = 0f;
        lookLockDuration = 0f;
        lookLockTarget = null;
        lookShakeIntensity = 0f;

        if (restoreCameraAfterJumpscare && fpsCamera != null && tpsCamera != null)
        {
            isFPS = savedIsFPSBeforeJumpscare;
            fpsCamera.SetActive(isFPS);
            tpsCamera.SetActive(!isFPS);
            restoreCameraAfterJumpscare = false;
        }

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void UpdateLookLock()
    {
        if (lookLockTarget != null && playerBody != null)
        {
            Vector3 direction = lookLockTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                playerBody.rotation = Quaternion.LookRotation(direction);
        }

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        ApplyLookShake();

        lookLockTimer += Time.deltaTime;
        if (lookLockTimer >= lookLockDuration)
            StopJumpscare();
    }

    private void ApplyLookShake()
    {
        if (cameraPivot == null || lookShakeIntensity <= 0f)
            return;

        float shakeX = (Random.value - 0.5f) * 2f * lookShakeIntensity;
        float shakeZ = (Random.value - 0.5f) * 2f * lookShakeIntensity;
        cameraPivot.localRotation = Quaternion.Euler(xRotation + shakeX, 0f, shakeZ);
    }

    void UpdateInteractionUI()
    {
        if (interactionText == null) return;

        if (isHidden && hiddenInside != null)
        {
            interactionText.text = "Press F to Exit";
            interactionText.enabled = true;
            return;
        }

        if (currentInteractable != null)
        {
            interactionText.text = currentInteractable.GetInteractionPrompt();
            interactionText.enabled = true;
        }
        else
        {
            interactionText.enabled = false;
        }
    }

    void UpdateStaminaVignette()
    {
        if (staminaVignette == null) return;

        float normalized = currentStamina / maxStamina;
        float targetAlpha = 0f;

        if (normalized < vignetteStartNormalized)
        {
            float t = 1f - (normalized / vignetteStartNormalized);
            targetAlpha = t * vignetteMaxAlpha;
        }

        Color c = staminaVignette.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * vignetteFadeSpeed);
        staminaVignette.color = c;
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0f);

        UpdateHPStageIndicators();
        UpdateStatsUI();

        if (currentHP <= 0f)
            OnDeath();
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);

        UpdateHPStageIndicators();
        UpdateStatsUI();
    }

    void UpdateHPStageIndicators()
    {
        if (hpStageIndicators == null || hpStageIndicators.Length == 0)
            return;

        float hpPercent = currentHP / maxHP;

        int stage = 0;

        if (hpPercent <= 0f)
            stage = 5;
        else if (hpPercent <= 0.2f)
            stage = 4;
        else if (hpPercent <= 0.4f)
            stage = 3;
        else if (hpPercent <= 0.6f)
            stage = 2;
        else if (hpPercent <= 0.8f)
            stage = 1;
        else
            stage = 0;

        for (int i = 0; i < hpStageIndicators.Length; i++)
        {
            if (hpStageIndicators[i] != null)
            {
                hpStageIndicators[i].SetActive(i < stage);
            }
        }
    }

    void UpdateStatsUI()
    {
        if (hpFillImage != null)
        {
            float hpPercent = currentHP / maxHP;
            hpFillImage.fillAmount = hpPercent;
        }

        if (staminaFillImage != null)
        {
            float staminaPercent = currentStamina / maxStamina;
            staminaFillImage.fillAmount = staminaPercent;
        }
    }

    void OnDeath()
    {
        Debug.Log("Player died.");

        if (animator != null)
        {   
            animator.SetBool("IsDead", true);
        }

        currentMove = Vector3.zero;
        moveInput = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Transform source = interactSource != null ? interactSource : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(source.position, source.forward * interactDistance);
    }

    void HandleDance()
    {
        if (animator == null)
            return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        bool canDance =
            isGrounded &&
            !isMoving &&
            !isRunning &&
            !isCrouching &&
            !isHidden &&
            !isReading;

        if (Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame &&
            canDance)
        {
            isDancing = !isDancing;
            animator.SetBool("IsDancing", isDancing);
        }

        // stop dancing automatically
        if (!canDance)
        {
            isDancing = false;
            animator.SetBool("IsDancing", false);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        bool walking = isMoving && !isRunning && !isCrouching;
        bool crouchWalking = isMoving && isCrouching;

        animator.SetBool("IsWalking", walking);
        animator.SetBool("IsRunning", isRunning && !isCrouching);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsCrouchWalking", crouchWalking);
        animator.SetBool("IsDead", currentHP <= 0f);
        animator.SetFloat("MoveSpeed", currentMove.magnitude);

        // FORCE model to stay at local position (fixes floating model)
        if (animator.transform.localPosition.y != 0f)
        {
            Vector3 pos = animator.transform.localPosition;
            pos.y = 0f;
            animator.transform.localPosition = pos;
        }
    }
}

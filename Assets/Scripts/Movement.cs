using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
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

    [Header("HP")]
    public float maxHP = 100f;

    [Header("UI")]
    public TextMeshProUGUI interactionText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI hpText;

    [Header("Low Stamina Vignette")]
    public Image staminaVignette;
    [Range(0f, 1f)] public float vignetteStartNormalized = 0.4f;
    [Range(0f, 1f)] public float vignetteMaxAlpha = 0.65f;
    public float vignetteFadeSpeed = 6f;

    private CharacterController controller;
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
    }

    void Update()
    {
        DetectInteractable();
        HandleInteraction();
        HandleGroundCheck();
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

        HandleGravity();
        UpdateInteractionUI();
        UpdateStatsUI();
        UpdateStaminaVignette();
    }

    void DetectInteractable()
    {
        currentInteractable = null;

        if (isHidden)
            return;

        Transform source = interactSource != null ? interactSource : transform;
        Ray ray = new Ray(source.position, source.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            MonoBehaviour[] behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    currentInteractable = interactable;
                    break;
                }
            }
        }
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
        if (isReading)
            return;

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
        if (isHidden || isReading) return;

        if (isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void HandleCrouch()
    {
        bool crouchPressed = false;

        if (inputActions != null)
            crouchPressed = inputActions.Player.Crouch.IsPressed();

        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
            crouchPressed = true;

        isCrouching = crouchPressed && isGrounded && !isHidden && !isReading;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

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
        if (isHidden || isReading) return;
        if (fpsCamera == null || tpsCamera == null) return;

        isFPS = !isFPS;
        fpsCamera.SetActive(isFPS);
        tpsCamera.SetActive(!isFPS);
    }

    public void EnterHide(HideableObject hideable)
    {
        if (hideable == null || hideable.hidePoint == null || hideable.exitPoint == null)
        {
            Debug.LogWarning("HideableObject is missing hidePoint or exitPoint.");
            return;
        }

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

    public void StopJumpscare()
    {
        isLookLocked = false;
        lookLockTimer = 0f;
        lookLockDuration = 0f;
        lookLockTarget = null;
        lookShakeIntensity = 0f;

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

        if (currentHP <= 0f)
            OnDeath();
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);
    }

    void OnDeath()
    {
        Debug.Log("Player died.");
    }

    void UpdateStatsUI()
    {
        if (staminaText != null)
            staminaText.text = $"Stamina: {Mathf.CeilToInt(currentStamina)}";

        if (hpText != null)
            hpText.text = $"HP: {Mathf.CeilToInt(currentHP)}";
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
}
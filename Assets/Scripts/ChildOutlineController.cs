using UnityEngine;

public class ChildOutlineController : MonoBehaviour
{
    public Outline outlineComponent;
    public Transform distanceCheckPoint;
    public float showDistance = 3f;
    public bool forceDisableAtStart = true;
    
    private Transform player;
    private bool isShowing = false;
    private float checkTimer = 0f;
    private float checkInterval = 0.2f; // Check every 0.2 seconds

    void Awake()
    {
        if (outlineComponent == null)
            outlineComponent = GetComponent<Outline>();
    }

    void Start()
    {
        // FORCE disable
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            isShowing = false;
        }

        if (distanceCheckPoint == null)
            distanceCheckPoint = transform.parent != null ? transform.parent : transform;

        FindPlayer();
    }

    void Update()
    {
        if (outlineComponent == null)
            return;

        // Check periodically, not every frame
        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f)
            return;
        checkTimer = checkInterval;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distance = Vector3.Distance(distanceCheckPoint.position, player.position);
        bool shouldShow = distance <= showDistance;

        if (shouldShow != isShowing)
        {
            isShowing = shouldShow;
            outlineComponent.enabled = shouldShow;
            Debug.Log($"[{name}] Outline {(shouldShow ? "ON" : "OFF")} - Distance: {distance:F1}");
        }
    }

    void FindPlayer()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm.transform;
        else
            player = null;
    }

    void OnDisable()
    {
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            isShowing = false;
        }
    }

    void OnDestroy()
    {
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }

    // Safety: Reset every time it's enabled
    void OnEnable()
    {
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            isShowing = false;
        }
        checkTimer = 0f;
    }
}
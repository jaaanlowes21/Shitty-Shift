using System.Collections;
using UnityEngine;

public class BreakerPanelRaycast : InteractableBase
{
    public enum SlideSpace
    {
        Local,
        World
    }

    [Header("Electrical Puzzle")]
    public ElectricalPowerPuzzle puzzle;

    [Header("Buttons")]
    [Tooltip("Assign the 16 button colliders in order. Element index must match the breaker button number.")]
    public Collider[] buttonColliders = new Collider[16];

    [Tooltip("Optional visible switch pieces to slide. If empty, the button collider transform slides.")]
    public Transform[] buttonSlideTargets = new Transform[16];

    [Tooltip("Optional outline/highlight objects for each button. These are enabled only while looking at that button.")]
    public GameObject[] buttonHighlightOutlines = new GameObject[16];

    [Tooltip("How far from the player camera the panel can raycast to find a button.")]
    public float buttonRaycastDistance = 4f;

    [Tooltip("Optional ray origin. Assign the FPS camera or camera pivot here so breaker aiming comes from the player's eyes.")]
    public Transform raycastSource;

    [Tooltip("Use Camera.main before falling back to the player's interact source.")]
    public bool preferMainCamera = true;

    [Tooltip("Shows the ray used to find the looked-at breaker button in the Scene view while playing.")]
    public bool drawDebugRay = true;

    [Header("Visuals")]
    [Tooltip("Use material changes for hover/selection. Turn this off when using outline objects.")]
    public bool useMaterialHighlight = false;
    public Material normalMaterial;
    public Material highlightMaterial;
    [Tooltip("Optional material used while a switch is selected. Leave empty if the slide movement is enough.")]
    public Material selectedMaterial;

    [Header("Switch Movement")]
    [Tooltip("Local movement applied when a breaker button is pressed. Use X for left/right movement.")]
    public Vector3 pressedLocalOffset = new Vector3(0.08f, 0f, 0f);
    [Tooltip("World movement applied when Slide Space is World. Useful when imported switches have different local axes.")]
    public Vector3 pressedWorldOffset = new Vector3(0.08f, 0f, 0f);
    public SlideSpace slideSpace = SlideSpace.World;
    public float slideSpeed = 12f;
    public bool wrongButtonsReturnToStart = true;

    private Renderer[] buttonRenderers;
    private Material[] originalMaterials;
    private Vector3[] originalLocalPositions;
    private Vector3[] originalWorldPositions;
    private Coroutine[] slideCoroutines;

    private PlayerMovement player;
    private int highlightedButtonIndex = -1;
    private int selectedButtonIndex = -1;

    private void Awake()
    {
        CacheButtons();
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        HideAllOutlines();
    }

    private void Update()
    {
        UpdateHighlightedButton();
    }

    private void Reset()
    {
        interactionPrompt = "Press F to Use Breaker";
        interactionRadius = 3f;
    }

    public override string GetInteractionPrompt()
    {
        if (puzzle != null && puzzle.IsBreakerSolved)
            return "Correct Breaker Set";

        return interactionPrompt;
    }

    public override void Interact(PlayerMovement player)
    {
        if (puzzle == null || player == null)
            return;

        this.player = player;

        int buttonIndex = GetLookedAtButtonIndex(GetPlayerRaySource(player));

        if (buttonIndex < 0)
        {
            HintManager.Instance?.ShowHint("I need to aim at one breaker button.");
            return;
        }

        TrySelectButton(buttonIndex);
    }

    public bool TrySelectButton(int buttonIndex)
    {
        if (puzzle == null)
            return false;

        if (buttonIndex < 0 || buttonIndex >= buttonColliders.Length || buttonColliders[buttonIndex] == null)
            return false;

        bool accepted = puzzle.PressBreakerButton(buttonIndex);

        if (!accepted)
            return false;

        SelectSwitch(buttonIndex);
        return true;
    }

    private void CacheButtons()
    {
        if (buttonColliders == null)
            buttonColliders = new Collider[0];

        buttonRenderers = new Renderer[buttonColliders.Length];
        originalMaterials = new Material[buttonColliders.Length];
        originalLocalPositions = new Vector3[buttonColliders.Length];
        originalWorldPositions = new Vector3[buttonColliders.Length];
        slideCoroutines = new Coroutine[buttonColliders.Length];

        for (int i = 0; i < buttonColliders.Length; i++)
        {
            Collider buttonCollider = buttonColliders[i];
            if (buttonCollider == null)
                continue;

            Renderer buttonRenderer = buttonCollider.GetComponent<Renderer>();
            if (buttonRenderer == null)
                buttonRenderer = buttonCollider.GetComponentInChildren<Renderer>();

            buttonRenderers[i] = buttonRenderer;
            Transform slideTarget = GetButtonSlideTarget(i);
            originalLocalPositions[i] = slideTarget.localPosition;
            originalWorldPositions[i] = slideTarget.position;

            if (buttonRenderer != null)
                originalMaterials[i] = buttonRenderer.material;
        }
    }

    private void UpdateHighlightedButton()
    {
        if (puzzle == null || puzzle.IsBreakerSolved)
        {
            ClearHighlightedButton();
            return;
        }

        Transform source = GetCurrentRaySource();
        int lookedButtonIndex = GetLookedAtButtonIndex(source);

        if (lookedButtonIndex == highlightedButtonIndex)
            return;

        ClearHighlightedButton();

        highlightedButtonIndex = lookedButtonIndex;

        if (highlightedButtonIndex >= 0 && highlightedButtonIndex != selectedButtonIndex)
        {
            if (useMaterialHighlight)
                SetButtonMaterial(highlightedButtonIndex, highlightMaterial);

            SetOutlineActive(highlightedButtonIndex, true);
        }
    }

    private void ClearHighlightedButton()
    {
        if (highlightedButtonIndex < 0 || highlightedButtonIndex == selectedButtonIndex)
        {
            highlightedButtonIndex = -1;
            return;
        }

        if (useMaterialHighlight)
            SetButtonMaterial(highlightedButtonIndex, GetNormalMaterial(highlightedButtonIndex));

        SetOutlineActive(highlightedButtonIndex, false);
        highlightedButtonIndex = -1;
    }

    private Transform GetCurrentRaySource()
    {
        if (raycastSource != null)
            return raycastSource;

        if (preferMainCamera && Camera.main != null)
            return Camera.main.transform;

        if (player != null)
            return GetPlayerRaySource(player);

        return transform;
    }

    private Transform GetPlayerRaySource(PlayerMovement player)
    {
        if (raycastSource != null)
            return raycastSource;

        if (preferMainCamera && Camera.main != null)
            return Camera.main.transform;

        return player.interactSource != null ? player.interactSource : player.transform;
    }

    private int GetLookedAtButtonIndex(Transform source)
    {
        if (source == null)
            return -1;

        if (drawDebugRay)
            Debug.DrawRay(source.position, source.forward * buttonRaycastDistance, Color.cyan);

        RaycastHit[] hits = Physics.RaycastAll(
            source.position,
            source.forward,
            buttonRaycastDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return -1;

        int bestButtonIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < buttonColliders.Length; i++)
        {
            Collider buttonCollider = buttonColliders[i];

            if (buttonCollider == null)
                continue;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == buttonCollider || hit.collider.transform.IsChildOf(buttonCollider.transform))
                {
                    if (hit.distance < bestDistance)
                    {
                        bestDistance = hit.distance;
                        bestButtonIndex = i;
                    }
                }
            }
        }

        return bestButtonIndex;
    }

    private void SetButtonMaterial(int buttonIndex, Material material)
    {
        if (material == null || buttonIndex < 0 || buttonIndex >= buttonColliders.Length)
            return;

        Renderer buttonRenderer = GetButtonRenderer(buttonIndex);
        if (buttonRenderer == null)
            return;

        buttonRenderer.material = material;
    }

    private void SetOutlineActive(int buttonIndex, bool active)
    {
        if (buttonHighlightOutlines == null || buttonIndex < 0 || buttonIndex >= buttonHighlightOutlines.Length)
            return;

        if (buttonHighlightOutlines[buttonIndex] != null)
            buttonHighlightOutlines[buttonIndex].SetActive(active);
    }

    private void HideAllOutlines()
    {
        if (buttonHighlightOutlines == null)
            return;

        foreach (GameObject outline in buttonHighlightOutlines)
        {
            if (outline != null)
                outline.SetActive(false);
        }
    }

    private Renderer GetButtonRenderer(int buttonIndex)
    {
        if (buttonRenderers == null || buttonIndex < 0 || buttonIndex >= buttonRenderers.Length)
            return null;

        return buttonRenderers[buttonIndex];
    }

    private Material GetNormalMaterial(int buttonIndex)
    {
        if (normalMaterial != null)
            return normalMaterial;

        if (originalMaterials != null && buttonIndex >= 0 && buttonIndex < originalMaterials.Length)
            return originalMaterials[buttonIndex];

        return null;
    }

    private void SlideButton(int buttonIndex, bool stayPressed)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonColliders.Length || buttonColliders[buttonIndex] == null)
            return;

        if (slideCoroutines != null && slideCoroutines[buttonIndex] != null)
            StopCoroutine(slideCoroutines[buttonIndex]);

        slideCoroutines[buttonIndex] = StartCoroutine(SlideButtonRoutine(buttonIndex, stayPressed));
    }

    private void SelectSwitch(int buttonIndex)
    {
        if (selectedButtonIndex >= 0 && selectedButtonIndex != buttonIndex)
            ResetSwitch(selectedButtonIndex);

        selectedButtonIndex = buttonIndex;
        SetOutlineActive(buttonIndex, false);
        highlightedButtonIndex = -1;

        if (useMaterialHighlight)
            SetButtonMaterial(buttonIndex, selectedMaterial != null ? selectedMaterial : GetNormalMaterial(buttonIndex));

        SlideButton(buttonIndex, true);
    }

    public void ResetSelectedSwitch()
    {
        if (selectedButtonIndex < 0)
            return;

        ResetSwitch(selectedButtonIndex);
        selectedButtonIndex = -1;
    }

    private void ResetSwitch(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonColliders.Length || buttonColliders[buttonIndex] == null)
            return;

        if (useMaterialHighlight)
            SetButtonMaterial(buttonIndex, GetNormalMaterial(buttonIndex));

        SetOutlineActive(buttonIndex, false);

        if (slideCoroutines != null && slideCoroutines[buttonIndex] != null)
            StopCoroutine(slideCoroutines[buttonIndex]);

        slideCoroutines[buttonIndex] = StartCoroutine(MoveButtonToRestPosition(buttonIndex));
    }

    private IEnumerator SlideButtonRoutine(int buttonIndex, bool stayPressed)
    {
        Transform buttonTransform = GetButtonSlideTarget(buttonIndex);
        Vector3 pressedPosition = GetPressedPosition(buttonIndex);

        yield return MoveButton(buttonTransform, pressedPosition);

        if (!stayPressed && wrongButtonsReturnToStart)
            yield return MoveButtonToRestPosition(buttonIndex);
    }

    private IEnumerator MoveButton(Transform buttonTransform, Vector3 targetLocalPosition)
    {
        bool useWorldSpace = slideSpace == SlideSpace.World;

        while (Vector3.Distance(useWorldSpace ? buttonTransform.position : buttonTransform.localPosition, targetLocalPosition) > 0.001f)
        {
            if (useWorldSpace)
            {
                buttonTransform.position = Vector3.Lerp(
                    buttonTransform.position,
                    targetLocalPosition,
                    Time.deltaTime * slideSpeed
                );
            }
            else
            {
                buttonTransform.localPosition = Vector3.Lerp(
                    buttonTransform.localPosition,
                    targetLocalPosition,
                    Time.deltaTime * slideSpeed
                );
            }

            yield return null;
        }

        if (useWorldSpace)
            buttonTransform.position = targetLocalPosition;
        else
            buttonTransform.localPosition = targetLocalPosition;
    }

    private IEnumerator MoveButtonToRestPosition(int buttonIndex)
    {
        yield return MoveButton(GetButtonSlideTarget(buttonIndex), GetOriginalPosition(buttonIndex));
    }

    private Vector3 GetPressedPosition(int buttonIndex)
    {
        if (slideSpace == SlideSpace.World)
            return GetOriginalPosition(buttonIndex) + pressedWorldOffset;

        return GetOriginalPosition(buttonIndex) + pressedLocalOffset;
    }

    private Vector3 GetOriginalPosition(int buttonIndex)
    {
        if (slideSpace == SlideSpace.World)
        {
            if (originalWorldPositions != null && buttonIndex >= 0 && buttonIndex < originalWorldPositions.Length)
                return originalWorldPositions[buttonIndex];

            return GetButtonSlideTarget(buttonIndex).position;
        }

        return GetOriginalLocalPosition(buttonIndex);
    }

    private Vector3 GetOriginalLocalPosition(int buttonIndex)
    {
        if (originalLocalPositions != null && buttonIndex >= 0 && buttonIndex < originalLocalPositions.Length)
            return originalLocalPositions[buttonIndex];

        return GetButtonSlideTarget(buttonIndex).localPosition;
    }

    private Transform GetButtonSlideTarget(int buttonIndex)
    {
        if (buttonSlideTargets != null &&
            buttonIndex >= 0 &&
            buttonIndex < buttonSlideTargets.Length &&
            buttonSlideTargets[buttonIndex] != null)
        {
            return buttonSlideTargets[buttonIndex];
        }

        return buttonColliders[buttonIndex].transform;
    }

    private void OnValidate()
    {
        if (buttonRaycastDistance < 0.1f)
            buttonRaycastDistance = 0.1f;

        if (slideSpeed < 0.1f)
            slideSpeed = 0.1f;
    }
}

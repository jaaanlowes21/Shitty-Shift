using UnityEngine;
using UnityEngine.InputSystem;

public class ClickAnywhereSpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject clickEffectPrefab;
    public Canvas targetCanvas;

    private RectTransform canvasRect;

    private void Awake()
    {
        if (targetCanvas != null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();

        Debug.Log("ClickAnywhereSpawner is active.");
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse clicked.");
            SpawnClickEffect();
        }
    }

    private void SpawnClickEffect()
    {
        if (clickEffectPrefab == null || targetCanvas == null || canvasRect == null)
        {
            Debug.LogWarning("Missing prefab, canvas, or canvas RectTransform.");
            return;
        }

        GameObject effect = Instantiate(clickEffectPrefab, targetCanvas.transform);

        RectTransform effectRect = effect.GetComponent<RectTransform>();

        if (effectRect == null)
        {
            Debug.LogWarning("Click prefab needs RectTransform.");
            Destroy(effect);
            return;
        }

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Mouse.current.position.ReadValue(),
            null,
            out localPoint
        );

        effectRect.anchoredPosition = localPoint;

        Debug.Log("Spawned click effect.");
    }
}
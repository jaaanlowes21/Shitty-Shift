using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverSpriteAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image targetImage;
    public Sprite[] hoverSprites;
    public float frameTime = 0.08f;
    [Range(0f, 1f)] public float hoverAlpha = 1f;

    private Coroutine routine;

    private void Start()
    {
        if (targetImage != null)
            targetImage.raycastTarget = false; // IMPORTANT

        SetAlpha(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage == null || hoverSprites == null || hoverSprites.Length == 0)
            return;

        SetAlpha(hoverAlpha);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(LoopAnimation());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetAlpha(0f);
    }

    private IEnumerator LoopAnimation()
    {
        int index = 0;

        while (true)
        {
            targetImage.sprite = hoverSprites[index];
            index = (index + 1) % hoverSprites.Length;
            yield return new WaitForSeconds(frameTime);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (targetImage == null) return;

        Color c = targetImage.color;
        c.a = alpha;
        targetImage.color = c;
    }
}
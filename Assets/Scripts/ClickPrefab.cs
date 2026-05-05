using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClickEffectAnimation : MonoBehaviour
{
    [Header("Image")]
    public Image targetImage;

    [Header("Animation")]
    public Sprite[] frames;
    public float frameTime = 0.05f;
    public int loopCount = 3;

    [Header("Fade Out")]
    public float fadeDuration = 0.25f;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Start()
    {
        StartCoroutine(PlayEffect());
    }

    private IEnumerator PlayEffect()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
        {
            Debug.LogWarning("ClickEffectAnimation missing Image or frames.");
            Destroy(gameObject);
            yield break;
        }

        SetAlpha(1f);

        for (int loop = 0; loop < loopCount; loop++)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                targetImage.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (targetImage == null) return;

        Color c = targetImage.color;
        c.a = alpha;
        targetImage.color = c;
    }
}
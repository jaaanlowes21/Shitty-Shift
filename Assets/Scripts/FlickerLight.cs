using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [Header("Light Colors")]
    public Color colorA = Color.white;
    public Color colorB = Color.red;

    [Header("Flicker Timing")]
    public float minFlickerTime = 0.05f;
    public float maxFlickerTime = 0.2f;

    [Header("Intensity")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;

    [Header("Smooth Flicker")]
    public bool smoothTransition = false;
    public float transitionSpeed = 10f;

    private Light lightSource;
    private Color targetColor;
    private float targetIntensity;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
    }

    private void Start()
    {
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Pick random color (A or B)
            targetColor = (Random.value > 0.5f) ? colorA : colorB;

            // Random intensity
            targetIntensity = Random.Range(minIntensity, maxIntensity);

            float waitTime = Random.Range(minFlickerTime, maxFlickerTime);

            if (!smoothTransition)
            {
                lightSource.color = targetColor;
                lightSource.intensity = targetIntensity;
            }
            else
            {
                StartCoroutine(SmoothTransition(targetColor, targetIntensity));
            }

            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator SmoothTransition(Color newColor, float newIntensity)
    {
        while (ColorDistance(lightSource.color, newColor) > 0.01f ||
               Mathf.Abs(lightSource.intensity - newIntensity) > 0.01f)
        {
            lightSource.color = Color.Lerp(lightSource.color, newColor, Time.deltaTime * transitionSpeed);
            lightSource.intensity = Mathf.Lerp(lightSource.intensity, newIntensity, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) +
                    Mathf.Abs(a.g - b.g) +
                    Mathf.Abs(a.b - b.b);
        }

        lightSource.color = newColor;
        lightSource.intensity = newIntensity;
    }
}
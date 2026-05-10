using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorrorLightFlicker : MonoBehaviour
{
    [Header("Light Array")]
    public Light[] flickerLights;
    
    [Header("Flicker Settings")]
    [Tooltip("Minimum intensity during flicker")]
    public float minIntensity = 0f;
    
    [Tooltip("Maximum intensity during flicker")]
    public float maxIntensity = 1.5f;
    
    [Tooltip("How fast the light flickers")]
    public float flickerSpeed = 0.1f;
    
    [Tooltip("Chance of a sudden dark flicker (0-1)")]
    [Range(0f, 1f)] public float darkFlickerChance = 0.3f;
    
    [Tooltip("Duration of sudden dark flickers")]
    public float darkFlickerDuration = 0.05f;
    
    [Tooltip("Minimum time between flicker patterns")]
    public float minFlickerInterval = 0.5f;
    
    [Tooltip("Maximum time between flicker patterns")]
    public float maxFlickerInterval = 3f;

    [Header("Smooth Flicker")]
    [Tooltip("Use smooth sine-based flickering")]
    public bool useSmoothFlicker = true;
    
    [Tooltip("Speed of smooth flicker waves")]
    public float smoothFlickerSpeed = 5f;
    
    [Tooltip("Amplitude of smooth flicker")]
    [Range(0f, 1f)] public float smoothFlickerAmplitude = 0.3f;

    [Header("Randomization")]
    [Tooltip("Randomize flicker for each light independently")]
    public bool independentFlicker = false;

    private float[] baseIntensities;
    private float[] flickerTimers;
    private float[] flickerIntervals;
    private Coroutine[] flickerCoroutines;

    void Start()
    {
        if (flickerLights == null || flickerLights.Length == 0)
        {
            // Auto-detect all lights in children if array is empty
            flickerLights = GetComponentsInChildren<Light>();
            
            if (flickerLights.Length == 0)
            {
                Debug.LogWarning("HorrorLightFlicker: No lights assigned or found!");
                enabled = false;
                return;
            }
        }

        // Store original intensities
        baseIntensities = new float[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                baseIntensities[i] = flickerLights[i].intensity;
            }
        }

        // Initialize timers
        flickerTimers = new float[flickerLights.Length];
        flickerIntervals = new float[flickerLights.Length];
        
        for (int i = 0; i < flickerLights.Length; i++)
        {
            flickerIntervals[i] = Random.Range(minFlickerInterval, maxFlickerInterval);
        }

        // Start flicker coroutines
        if (independentFlicker)
        {
            flickerCoroutines = new Coroutine[flickerLights.Length];
            for (int i = 0; i < flickerLights.Length; i++)
            {
                flickerCoroutines[i] = StartCoroutine(IndependentFlicker(i));
            }
        }
    }

    void Update()
    {
        if (!independentFlicker)
        {
            // Update all lights together
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] == null) continue;
                
                flickerTimers[i] += Time.deltaTime;
                
                if (flickerTimers[i] >= flickerIntervals[i])
                {
                    flickerTimers[i] = 0f;
                    flickerIntervals[i] = Random.Range(minFlickerInterval, maxFlickerInterval);
                    StartCoroutine(FlickerLight(i));
                }
                
                // Smooth flicker background
                if (useSmoothFlicker)
                {
                    float smoothNoise = Mathf.PerlinNoise(Time.time * smoothFlickerSpeed, i * 100f);
                    float smoothFlicker = 1f - (smoothNoise * smoothFlickerAmplitude);
                    flickerLights[i].intensity = baseIntensities[i] * smoothFlicker;
                }
            }
        }
    }

    IEnumerator FlickerLight(int index)
    {
        if (flickerLights[index] == null) yield break;

        // Random chance of a dark flicker (lights almost off)
        if (Random.value < darkFlickerChance)
        {
            flickerLights[index].intensity = baseIntensities[index] * minIntensity;
            yield return new WaitForSeconds(darkFlickerDuration);
            flickerLights[index].intensity = baseIntensities[index] * maxIntensity;
            yield return new WaitForSeconds(darkFlickerDuration * 0.5f);
            flickerLights[index].intensity = baseIntensities[index] * minIntensity;
            yield return new WaitForSeconds(darkFlickerDuration);
        }

        // Rapid flicker pattern
        int flickerCount = Random.Range(2, 6);
        for (int i = 0; i < flickerCount; i++)
        {
            float randomIntensity = Random.Range(minIntensity, maxIntensity);
            flickerLights[index].intensity = baseIntensities[index] * randomIntensity;
            yield return new WaitForSeconds(Random.Range(0.02f, flickerSpeed));
        }

        // Return to normal
        flickerLights[index].intensity = baseIntensities[index];
    }

    IEnumerator IndependentFlicker(int index)
    {
        while (true)
        {
            if (flickerLights[index] == null) yield break;

            // Wait random time between flickers
            float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            yield return new WaitForSeconds(waitTime);

            // Random chance of a dark flicker
            if (Random.value < darkFlickerChance)
            {
                flickerLights[index].intensity = baseIntensities[index] * minIntensity;
                yield return new WaitForSeconds(darkFlickerDuration);
                flickerLights[index].intensity = baseIntensities[index] * maxIntensity;
                yield return new WaitForSeconds(darkFlickerDuration * 0.5f);
                flickerLights[index].intensity = baseIntensities[index] * minIntensity;
                yield return new WaitForSeconds(darkFlickerDuration);
            }

            // Rapid flicker pattern
            int flickerCount = Random.Range(2, 6);
            for (int i = 0; i < flickerCount; i++)
            {
                float randomIntensity = Random.Range(minIntensity, maxIntensity);
                flickerLights[index].intensity = baseIntensities[index] * randomIntensity;
                yield return new WaitForSeconds(Random.Range(0.02f, flickerSpeed));
            }

            // Return to normal
            flickerLights[index].intensity = baseIntensities[index];
            
            // Smooth flicker during wait
            if (useSmoothFlicker)
            {
                float smoothTimer = 0f;
                while (smoothTimer < waitTime)
                {
                    smoothTimer += Time.deltaTime;
                    float smoothNoise = Mathf.PerlinNoise(Time.time * smoothFlickerSpeed, index * 100f);
                    float smoothFlicker = 1f - (smoothNoise * smoothFlickerAmplitude);
                    flickerLights[index].intensity = baseIntensities[index] * smoothFlicker;
                    yield return null;
                }
            }
        }
    }

    // Public method to trigger a flicker on demand
    public void TriggerFlicker()
    {
        for (int i = 0; i < flickerLights.Length; i++)
        {
            StartCoroutine(FlickerLight(i));
        }
    }

    // Public method to turn all lights off briefly (jumpscare effect)
    public void Blackout(float duration)
    {
        StartCoroutine(BlackoutCoroutine(duration));
    }

    IEnumerator BlackoutCoroutine(float duration)
    {
        // Store current intensities
        float[] currentIntensities = new float[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                currentIntensities[i] = flickerLights[i].intensity;
                flickerLights[i].intensity = 0f;
            }
        }

        yield return new WaitForSeconds(duration);

        // Restore intensities
        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                flickerLights[i].intensity = currentIntensities[i];
            }
        }
    }

    private void OnDisable()
    {
        // Restore original intensities when disabled
        if (baseIntensities != null)
        {
            for (int i = 0; i < flickerLights.Length; i++)
            {
                if (flickerLights[i] != null && i < baseIntensities.Length)
                {
                    flickerLights[i].intensity = baseIntensities[i];
                }
            }
        }
    }
}
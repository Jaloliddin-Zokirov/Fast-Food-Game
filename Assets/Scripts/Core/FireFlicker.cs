using UnityEngine;

// Olov yorug'ligini miltillatadi
public class FireFlicker : MonoBehaviour
{
    Light lamp;
    float baseIntensity;
    float seed;

    void Start()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        lamp.intensity = baseIntensity * (0.6f + 0.8f * Mathf.PerlinNoise(seed, Time.time * 8f));
    }
}

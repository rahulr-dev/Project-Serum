using UnityEngine;

public class EmisFlicker : MonoBehaviour
{
    [Header("Blink Settings")]
    [Tooltip("How many times per second the emission changes.")]
    [Min(0.01f)]
    public float frequency = 2f;

    [Tooltip("Minimum emission intensity.")]
    [Min(0f)]
    public float minIntensity = 0f;

    [Tooltip("Maximum emission intensity.")]
    [Min(0f)]
    public float maxIntensity = 5f;

    private Material material;
    private Color emissionColor;

    void Start()
    {
        material = GetComponent<Renderer>().material;

        if (material.HasProperty("_EmissionColor"))
        {
            emissionColor = material.GetColor("_EmissionColor");
        }
    }

    void Update()
    {
        if (material == null || !material.HasProperty("_EmissionColor"))
            return;

        // Switch ON/OFF based on frequency
        bool isOn = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) >= 0f;

        float intensity = isOn ? maxIntensity : minIntensity;

        material.SetColor("_EmissionColor", emissionColor * intensity);
    }
    }

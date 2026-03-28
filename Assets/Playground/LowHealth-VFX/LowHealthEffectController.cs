using UnityEngine;
using UnityEngine.Rendering;


public class LowHealthEffectController : MonoBehaviour
{
    public HealthComponent health;
    public Volume volume;

    [Range(0f, 1f)]
    public float threshold = 0.3f;

    void Start()
    {
        volume.profile = Instantiate(volume.profile);
        health.OnHealthChanged += UpdateEffect;
        volume.weight = 0f;
        UpdateEffect(health.currentHealth, health.maxHealth);
    }

    void UpdateEffect(int current, int max)
    {
        float percent = (float)current / max;

        float intensity = percent <= threshold
            ? 1f - (percent / threshold)
            : 0f;

        volume.weight = intensity;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= UpdateEffect;
    }
}
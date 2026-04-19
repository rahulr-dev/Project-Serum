using UnityEngine;
using UnityEngine.InputSystem;

// Simple test script to apply damage and healing to the HealthComponent using keyboard input
public class TestDamageInput : MonoBehaviour
{
    public Health health;

    void Update()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            health.TakeDamage(10);
            Debug.Log("Damage taken");
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            health.Heal(10);
            Debug.Log("Healed");
        }
    }
}

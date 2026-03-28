using System;
using System.Collections;
using UnityEngine;

// Interface for damageable entities
public interface IDamageable
{
    void TakeDamage(int amount);
}

public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    [Header("Defense")]
    public int defense = 0; // reduces incoming damage for shielding mechanics

    [Header("Options")]
    public float invincibleTime = 0.3f; // after taking damage, prevents further damage for this duration in seconds

    // Events for other systems (Effects, AI, etc.)
    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private bool isDead = false;
    private bool isInvincible = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Applies damage to the entity, factoring in defense and invincibility
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return; // Ignore non-positive damage
        if (isDead || isInvincible) return;

        StartCoroutine(Invincibility());

        int finalDamage = Mathf.Max(amount - defense, 1);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Heal function
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Death logic
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
    }

    // Temporary invulnerability
    private IEnumerator Invincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    // Returns health percentage (0–1)
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
}
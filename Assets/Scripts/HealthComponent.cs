using System;
using System.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
}

public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Defense")]
    public int defense = 0; // reduces incoming damage kinda shield

    [Header("Options")]
    public bool destroyOnDeath = true; // deletes the GameObject when health reaches zero
    public float invincibleTime = 0.3f; // after taking damage, prevents further damage for this duration in seconds

    // Events for other systems (Effects, AI, etc.)
    public Action<int, int> OnHealthChanged; // (current, max)
    public Action OnDeath;

    private bool isDead = false;
    private bool isInvincible = false;

    void Awake()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Called by any damage source
    public void TakeDamage(int amount)
    {
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

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
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
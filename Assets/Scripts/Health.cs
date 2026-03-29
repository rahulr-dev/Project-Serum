using System;
using System.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
    void Heal(int amount);
    float GetHealthPercent();
}

public class Health : MonoBehaviour, IDamageable
{
    [field: SerializeField] public int MaxHealth { get; private set; }
    public int currentHealth { get; private set; }

    [SerializeField] private int defense = 0;
    [SerializeField] private float invincibleTime = 0.3f;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private bool isDead = false;
    private bool isInvincible = false;

    void Awake()
    {
        currentHealth = MaxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (isDead || isInvincible) return;

        StartCoroutine(Invincibility());

        int finalDamage = Mathf.Max(amount - defense, 1);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
    }

    private IEnumerator Invincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / MaxHealth;
    }
}

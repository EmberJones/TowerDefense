using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealthValue;

    public float damageReductionMultiplier = 1f;

    public int CurrentHealth
    {
        get => currentHealthValue;
        set => currentHealthValue = value;
    }

    public int MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;

    private bool isDead;

    private void Awake()
    {
        currentHealthValue = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        int adjustedDamage = Mathf.RoundToInt(damageAmount * damageReductionMultiplier);
        currentHealthValue = Mathf.Max(currentHealthValue - adjustedDamage, 0);
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        if (currentHealthValue <= 0)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }

    public void ResetHealth()
    {
        currentHealthValue = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);
    }
}
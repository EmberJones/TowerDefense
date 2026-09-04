using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealthValue;

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

        currentHealthValue = Mathf.Max(currentHealthValue - damageAmount, 0);
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        if (currentHealthValue <= 0)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }
}
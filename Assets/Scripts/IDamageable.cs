using UnityEngine;

public interface IDamageable
{
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    void TakeDamage(int damageAmount);
}

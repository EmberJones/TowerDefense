using System;
using UnityEngine;

public class Crit : MonoBehaviour
{
    [Range(0f, 1f)] public float critChance = 0.25f;
    public float critMultiplier = 2f;

    [HideInInspector] public float bonusMultiplier;

    public float TotalMultiplier => critMultiplier + bonusMultiplier;

    public event Action<Transform, int> OnCrit;

    private Attacker attacker;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttackHit += HandleHit;
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttackHit -= HandleHit;
    }

    private void HandleHit(Transform target)
    {
        if (target == null) return;

        int baseDamage = attacker.attackDamage;
        if (!TryCrit(baseDamage, target, out int critDamage)) return;

        int extra = critDamage - baseDamage;
        if (extra <= 0) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(extra);
    }

    public bool TryCrit(int baseDamage, Transform target, out int finalDamage)
    {
        finalDamage = baseDamage;
        if (UnityEngine.Random.value >= critChance) return false;

        finalDamage = Mathf.CeilToInt(baseDamage * TotalMultiplier);
        OnCrit?.Invoke(target, finalDamage);
        return true;
    }
}
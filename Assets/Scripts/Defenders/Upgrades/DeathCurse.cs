using System;
using System.Collections.Generic;
using UnityEngine;

public class DeathCurseEffect : MonoBehaviour
{
    public float explosionRadius = 3f;
    public int explosionDamage = 15;
    public LayerMask targetLayer;

    public event Action<Vector3> OnCurseExplosion;

    private Attacker attacker;
    private HashSet<Health> curseTargets = new HashSet<Health>();

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

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth == null || curseTargets.Contains(targetHealth)) return;

        curseTargets.Add(targetHealth);

        void OnCursedDeath()
        {
            targetHealth.OnDeath -= OnCursedDeath;
            curseTargets.Remove(targetHealth);
            Explode(target.position);
        }

        targetHealth.OnDeath += OnCursedDeath;
    }

    private void Explode(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage(explosionDamage);
        }

        OnCurseExplosion?.Invoke(position);
    }
}
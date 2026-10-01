using System;
using UnityEngine;

public class CleaveOnHitEffect : MonoBehaviour
{
    public float cleaveRadius = 3f;
    public float CleaveMultiplier = 0.7f;
    public LayerMask targetLayer;

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

    private void HandleHit(Transform primaryTarget)
    {
        Collider[] hits = Physics.OverlapSphere(primaryTarget.position, cleaveRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.transform == primaryTarget) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage((int)Math.Ceiling(attacker.attackDamage * CleaveMultiplier));

            
        }
    }
}
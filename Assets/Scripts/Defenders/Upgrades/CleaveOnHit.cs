using System.Collections.Generic;
using UnityEngine;

public class CleaveOnHitEffect : MonoBehaviour
{
    public float cleaveRadius = 3f;
    public float CleaveMultiplier = 0.7f;
    public LayerMask targetLayer;

    [HideInInspector] public float radiusScale = 1f;
    [HideInInspector] public float damageScale = 1f;
    [HideInInspector] public bool canCrit;
    [HideInInspector] public Crit crit;

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
        if (primaryTarget == null) return;

        float radius = cleaveRadius * radiusScale;
        int baseDamage = Mathf.CeilToInt(attacker.attackDamage * CleaveMultiplier * damageScale);
        QuickVFX.Ring(primaryTarget.position, radius, new Color(1f, 0.45f, 0.15f), 0.3f);

        Collider[] hits = Physics.OverlapSphere(primaryTarget.position, radius, targetLayer);
        HashSet<Transform> damaged = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            if (hit.transform == primaryTarget || !damaged.Add(hit.transform)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            int damage = baseDamage;
            if (canCrit && crit != null)
                crit.TryCrit(baseDamage, hit.transform, out damage);

            damageable.TakeDamage(damage);
            QuickVFX.Burst(QuickVFX.Center(hit.transform), new Color(1f, 0.45f, 0.15f), 8, 3f, 0.14f, 0.3f);
        }
    }
}
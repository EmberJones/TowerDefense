using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightningEffect : MonoBehaviour
{
    public int maxChains = 2;
    public float chainRadius = 5f;
    public int chainDamage = 8;
    public LayerMask targetLayer;
    public Color boltColor = new Color(0.55f, 0.85f, 1f);
    public event Action<Transform> OnChainJump;

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

        HashSet<Transform> hitEnemies = new HashSet<Transform> { target };
        Transform current = target;

        QuickVFX.Burst(QuickVFX.Center(target), boltColor, 10, 3f, 0.12f, 0.25f);
        Vector3 origin = attacker.firePoint != null ? attacker.firePoint.position : attacker.transform.position + Vector3.up * 1.5f;
        QuickVFX.Bolt(origin, QuickVFX.Center(target), boltColor);

        for (int i = 0; i < maxChains; i++)
        {
            Transform next = FindNearestUnhit(current, hitEnemies);
            if (next == null) break;

            IDamageable damageable = next.GetComponent<IDamageable>();
            damageable?.TakeDamage(chainDamage);

            QuickVFX.Bolt(QuickVFX.Center(current), QuickVFX.Center(next), boltColor);
            QuickVFX.Burst(QuickVFX.Center(next), boltColor, 10, 3f, 0.12f, 0.25f);

            hitEnemies.Add(next);
            current = next;

            OnChainJump?.Invoke(next);
        }
    }

    private Transform FindNearestUnhit(Transform from, HashSet<Transform> exclude)
    {
        Collider[] hits = Physics.OverlapSphere(from.position, chainRadius, targetLayer);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            if (exclude.Contains(hit.transform)) continue;

            float d = Vector3.Distance(from.position, hit.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = hit.transform;
            }
        }

        return closest;
    }
}
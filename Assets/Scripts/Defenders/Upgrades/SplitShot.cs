using System.Collections.Generic;
using UnityEngine;

public class SplitShot : MonoBehaviour
{
    [SerializeField] private int totalTargets = 2;

    [SerializeField] private float extraSearchRadius = 0f;

    [SerializeField] private bool useConeFilter = true;

    [SerializeField] private float coneHalfAngle = 45f;

    //delay is ranged only
    [SerializeField] private float extraShotDelay = 0.07f;

    public event System.Action<Transform> OnExtraShotFired;

    private Attacker attacker;
    private readonly List<Transform> extraTargets = new List<Transform>();
    private bool resolvePending = false;
    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttack += HandleAttack;
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttack -= HandleAttack;
    }

    private void HandleAttack(Transform primary)
    {
        if (primary == null) return;

        CollectExtraTargets(primary);

        if (extraTargets.Count == 0) return;

        // Melee resolves immediately; ranged deferred flows through ResolveAttack.
        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            // Melee: ResolveAttack will be called by the animation event for the primary.
            // We queue extra strikes to fire at the same resolve moment.
            resolvePending = true;
        }
        else if (attacker.deferRangedResolution)
        {
            // Ranged with animation-event deferral: wait for ResolveAttack.
            resolvePending = true;
        }
        else
        {
            // Immediate ranged: fire extra projectiles right away.
            FireExtras();
        }
    }

    public void ResolveExtraTargets()
    {
        if (!resolvePending) return;
        resolvePending = false;
        FireExtras();
    }

    private void FireExtras()
    {
        if (extraTargets.Count == 0) return;

        if (extraShotDelay > 0f && attacker.attackType == Attacker.AttackType.Ranged)
            StartCoroutine(FireExtrasDelayed());
        else
            FireExtrasNow();
    }

    private System.Collections.IEnumerator FireExtrasDelayed()
    {
        yield return new WaitForSeconds(extraShotDelay);
        FireExtrasNow();
    }

    private void FireExtrasNow()
    {
        var targets = new List<Transform>(extraTargets);
        extraTargets.Clear();

        foreach (var t in targets)
        {
            if (t == null) continue;

            if (Vector3.Distance(transform.position, t.position) >
                attacker.attackRange + extraSearchRadius)
                continue;

            if (attacker.attackType == Attacker.AttackType.Melee)
            {
                var dmg = t.GetComponent<IDamageable>();
                dmg?.TakeDamage(attacker.attackDamage);
                attacker.SendMessage("HandleHit", t, SendMessageOptions.DontRequireReceiver);
                OnExtraShotFired?.Invoke(t);
            }
            else
            {
                attacker.SendMessage("FireRanged", t, SendMessageOptions.DontRequireReceiver);
                OnExtraShotFired?.Invoke(t);
            }
        }
    }

    private void CollectExtraTargets(Transform primary)
    {
        extraTargets.Clear();
        int needed = Mathf.Max(0, totalTargets - 1);
        if (needed == 0) return;

        float radius = attacker.attackRange + extraSearchRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, attacker.targetLayer);

        Vector3 forward = (primary.position - transform.position);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

        // Collect candidates with distance, excluding primary.
        var candidates = new List<(Transform t, float d)>();
        foreach (var hit in hits)
        {
            Transform t = hit.transform;
            if (t == primary) continue;

            float d = Vector3.Distance(transform.position, t.position);
            if (d > radius) continue;

            if (useConeFilter)
            {
                Vector3 dir = (t.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;
                float angle = Vector3.Angle(forward, dir);
                if (angle > coneHalfAngle) continue;
            }

            candidates.Add((t, d));
        }

        candidates.Sort((a, b) => a.d.CompareTo(b.d));

        for (int i = 0; i < candidates.Count && extraTargets.Count < needed; i++)
            extraTargets.Add(candidates[i].t);
    }
}
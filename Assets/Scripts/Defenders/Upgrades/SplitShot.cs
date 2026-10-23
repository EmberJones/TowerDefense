using System.Collections.Generic;
using UnityEngine;

public class SplitShot : MonoBehaviour
{
    [SerializeField] private int totalTargets = 2;
    [SerializeField] private float extraSearchRadius = 0f;
    [SerializeField] private bool useConeFilter = true;
    [SerializeField] private float coneHalfAngle = 45f;
    [SerializeField] private float extraShotDelay = 0.07f;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    public event System.Action<Transform> OnExtraShotFired;

    private Attacker attacker;
    private readonly List<Transform> extraTargets = new List<Transform>();
    private bool resolvePending = false;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttack += HandleAttack;

        if (debug)
            Debug.Log($"[SplitShot] Awake on '{name}', attacker={(attacker != null ? attacker.name : "NULL")}, totalTargets={totalTargets}");
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

        if (debug)
            Debug.Log($"[SplitShot] Attack on primary={primary.name}, collected {extraTargets.Count} extra target(s).");

        if (extraTargets.Count == 0) return;

        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            resolvePending = true;
            if (debug) Debug.Log("[SplitShot] Melee: extras queued for ResolveAttack.");
        }
        else if (attacker.deferRangedResolution)
        {
            resolvePending = true;
            if (debug) Debug.Log("[SplitShot] Ranged (deferred): extras queued for ResolveAttack.");
        }
        else
        {
            if (debug) Debug.Log("[SplitShot] Ranged (immediate): firing extras now.");
            FireExtras();
        }
    }

    public void ResolveExtraTargets()
    {
        if (!resolvePending)
        {
            if (debug) Debug.Log("[SplitShot] ResolveExtraTargets called but nothing pending.");
            return;
        }
        resolvePending = false;
        if (debug) Debug.Log("[SplitShot] ResolveExtraTargets -> firing queued extras.");
        FireExtras();
    }

    private void FireExtras()
    {
        if (extraTargets.Count == 0) return;

        if (extraShotDelay > 0f && attacker.attackType == Attacker.AttackType.Ranged)
        {
            if (debug) Debug.Log($"[SplitShot] Delaying extras by {extraShotDelay}s.");
            StartCoroutine(FireExtrasDelayed());
        }
        else
        {
            FireExtrasNow();
        }
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
            {
                if (debug) Debug.Log($"[SplitShot] Extra {t.name} skipped: out of range.");
                continue;
            }

            if (attacker.attackType == Attacker.AttackType.Melee)
            {
                var dmg = t.GetComponent<IDamageable>();
                dmg?.TakeDamage(attacker.attackDamage);
                attacker.SendMessage("HandleHit", t, SendMessageOptions.DontRequireReceiver);
                OnExtraShotFired?.Invoke(t);

                if (debug) Debug.Log($"[SplitShot] Melee extra hit {t.name} for {attacker.attackDamage}, event fired.");
            }
            else
            {
                attacker.SendMessage("FireRanged", t, SendMessageOptions.DontRequireReceiver);
                OnExtraShotFired?.Invoke(t);

                if (debug) Debug.Log($"[SplitShot] Ranged extra fired at {t.name}, event fired.");
            }
        }
    }

    private void CollectExtraTargets(Transform primary)
    {
        extraTargets.Clear();
        int needed = Mathf.Max(0, totalTargets - 1);
        if (needed == 0)
        {
            if (debug) Debug.Log("[SplitShot] totalTargets<=1, no extras needed.");
            return;
        }

        float radius = attacker.attackRange + extraSearchRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, attacker.targetLayer);

        Vector3 forward = (primary.position - transform.position);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = transform.forward;
        forward.Normalize();

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

        if (debug)
        {
            string list = extraTargets.Count > 0
                ? string.Join(", ", extraTargets.ConvertAll(t => t != null ? t.name : "null"))
                : "(none)";
            Debug.Log($"[SplitShot] Candidate search radius={radius:F2}, found {candidates.Count} candidate(s), picked {extraTargets.Count}: {list}");
        }
    }
}
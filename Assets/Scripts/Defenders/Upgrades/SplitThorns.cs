using System.Collections.Generic;
using UnityEngine;

public class ComboSharedThorns : MonoBehaviour
{
    [Tooltip("Cap on the summed thorns stack for this attack (0 = uncapped).")]
    [SerializeField] private int maxSummedStacks = 0;

    [Tooltip("Multiplier applied to the summed stack before dealing damage.")]
    [SerializeField] private float sharedStackMultiplier = 1f;

    [Tooltip("If true, LR only fires when SplitShot actually hit a second target.")]
    [SerializeField] private bool requireSplitShot = true;

    [Tooltip("Wait this long after the attack starts before resolving, so SplitShot extras land.")]
    [SerializeField] private float resolveDelay = 0.15f;

    private Attacker attacker;
    private SplitShot splitShot;
    private Thorns thorns;

    private readonly List<Transform> attackTargets = new List<Transform>();
    private bool attackInProgress = false;
    private Coroutine resolveRoutine;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        splitShot = GetComponentInParent<SplitShot>();
        thorns = GetComponentInParent<Thorns>();

        if (attacker != null)
        {
            attacker.OnAttack += HandleAttackStart;
            attacker.OnAttackHit += HandleHit;
        }
        if (splitShot != null)
            splitShot.OnExtraShotFired += HandleExtraFired;

        if (thorns != null)
            thorns.suppressNormalApplication = false;
    }

    private void OnDestroy()
    {
        if (attacker != null)
        {
            attacker.OnAttack -= HandleAttackStart;
            attacker.OnAttackHit -= HandleHit;
        }
        if (splitShot != null)
            splitShot.OnExtraShotFired -= HandleExtraFired;

        if (thorns != null)
            thorns.suppressNormalApplication = false;

        if (resolveRoutine != null) StopCoroutine(resolveRoutine);
    }

    private void HandleAttackStart(Transform primary)
    {
        attackTargets.Clear();
        if (primary != null) attackTargets.Add(primary);
        attackInProgress = true;

        if (resolveRoutine != null) StopCoroutine(resolveRoutine);
        resolveRoutine = StartCoroutine(ResolveAfterDelay());
    }

    private void HandleExtraFired(Transform extra)
    {
        if (!attackInProgress || extra == null) return;
        if (!attackTargets.Contains(extra))
            attackTargets.Add(extra);
    }

    private void HandleHit(Transform hit)
    {
        // also collect via OnAttackHit in case SplitShot's
        // extra-report path differs from the actual damage path.
        if (!attackInProgress || hit == null) return;
        if (!attackTargets.Contains(hit))
            attackTargets.Add(hit);
    }

    private System.Collections.IEnumerator ResolveAfterDelay()
    {
        // Let SplitShot's extras (which may be delayed) land
        // before we sum thorns.
        yield return new WaitForSeconds(resolveDelay);

        attackInProgress = false;

        // Gate on whether SplitShot actually produced a second target.
        if (requireSplitShot && attackTargets.Count < 2) yield break;

        // Sum thorns across the targets struck by attack.
        var liveTargets = new List<Transform>();
        foreach (var t in attackTargets)
            if (t != null) liveTargets.Add(t);

        if (liveTargets.Count == 0) yield break;

        int perTarget = thorns != null ? thorns.thornAmount : 1;
        int damagePerThorn = thorns != null ? thorns.damagePerThorn : 1;

        int summed = perTarget * liveTargets.Count;

        if (maxSummedStacks > 0 && summed > maxSummedStacks)
            summed = maxSummedStacks;

        summed = Mathf.RoundToInt(summed * sharedStackMultiplier);

        int totalDamage = summed * damagePerThorn;
        if (totalDamage <= 0) yield break;

        foreach (var t in liveTargets)
        {
            IDamageable dmg = t.GetComponent<IDamageable>();
            dmg?.TakeDamage(totalDamage);
        }

        attackTargets.Clear();
    }
}
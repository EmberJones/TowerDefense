using UnityEngine;

/// <summary>
/// MR Combo: Echo Attack deals bonus damage scaling with the target's thorns.
/// bonus = damagePerThornOnTarget * thornAmountOnTarget * bonusMultiplier (capped).
/// Self-contained: does NOT rely on DoubleShot's internals.
/// </summary>
public class ComboEchoThorns : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float procChance = 0.5f;

    [SerializeField] private float delay = 0.1f;

    [Tooltip("Base echo damage multiplier (echo = attackDamage * this).")]
    [SerializeField] private float baseMultiplier = 1f;

    [Tooltip("Multiplier applied to (damagePerThorn * thornAmount) on the target.")]
    [SerializeField] private float thornsBonusMultiplier = 1f;

    [Tooltip("Flat cap on the thorns bonus (0 = uncapped).")]
    [SerializeField] private int maxThornsBonus = 0;

    [Tooltip("If true, MR's echo replaces DoubleShot (set base DoubleShot.procChance=0 on the tower).")]
    [SerializeField] private bool suppressBaseDoubleShot = true;

    private Attacker attacker;
    private EchoAttack baseDoubleShot;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        baseDoubleShot = GetComponentInParent<EchoAttack>();

        if (suppressBaseDoubleShot && baseDoubleShot != null)
            baseDoubleShot.enabled = false; // stop the base echo from double-firing

        if (attacker != null)
            attacker.OnAttack += HandleAttack;
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttack -= HandleAttack;
        if (suppressBaseDoubleShot && baseDoubleShot != null)
            baseDoubleShot.enabled = true;
    }

    private void HandleAttack(Transform target)
    {
        if (target == null) return;
        if (Random.value > procChance) return;
        StartCoroutine(FireEcho(target));
    }

    private System.Collections.IEnumerator FireEcho(Transform originalTarget)
    {
        yield return null;
        yield return new WaitForSeconds(delay);

        Transform target = originalTarget != null ? originalTarget : attacker.CurrentTarget;
        if (target == null) yield break;
        if (Vector3.Distance(transform.position, target.position) > attacker.attackRange) yield break;

        int thornsBonus = 0;
        var status = target.GetComponent<ThornsStatus>();
        if (status != null)
        {
            thornsBonus = Mathf.RoundToInt(
                status.DamagePerThorn * status.ThornAmount * thornsBonusMultiplier);
        }
        if (maxThornsBonus > 0 && thornsBonus > maxThornsBonus)
            thornsBonus = maxThornsBonus;

        int baseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.attackDamage * baseMultiplier));
        int totalDamage = baseDamage + thornsBonus;

        int originalDamage = attacker.attackDamage;
        attacker.attackDamage = totalDamage;

        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            var dmg = target.GetComponent<IDamageable>();
            dmg?.TakeDamage(totalDamage);
            attacker.SendMessage("HandleHit", target, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            attacker.SendMessage("FireRanged", target, SendMessageOptions.DontRequireReceiver);
        }

        attacker.attackDamage = originalDamage;
    }
}
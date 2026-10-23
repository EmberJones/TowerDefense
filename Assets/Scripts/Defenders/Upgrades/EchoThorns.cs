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

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Attacker attacker;
    private EchoAttack baseDoubleShot;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        baseDoubleShot = GetComponentInParent<EchoAttack>();

        if (suppressBaseDoubleShot && baseDoubleShot != null)
        {
            baseDoubleShot.enabled = false;
            if (debug)
                Debug.Log($"[EchoThorns] Suppressed base EchoAttack on '{baseDoubleShot.name}'.");
        }

        if (attacker != null)
            attacker.OnAttack += HandleAttack;

        if (debug)
            Debug.Log($"[EchoThorns] Awake on '{name}', attacker={(attacker != null ? attacker.name : "NULL")}, " +
                      $"procChance={procChance}, baseMult={baseMultiplier}, thornsMult={thornsBonusMultiplier}, maxBonus={maxThornsBonus}");
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

        float roll = Random.value;
        bool proc = roll <= procChance;

        if (debug)
            Debug.Log($"[EchoThorns] OnAttack target={target.name}, roll={roll:F3} vs procChance={procChance:F3} => {(proc ? "PROC" : "no proc")}");

        if (!proc) return;

        StartCoroutine(FireEcho(target));
    }

    private System.Collections.IEnumerator FireEcho(Transform originalTarget)
    {
        yield return null;
        yield return new WaitForSeconds(delay);

        Transform target = originalTarget != null ? originalTarget : attacker.CurrentTarget;
        if (target == null)
        {
            if (debug) Debug.Log("[EchoThorns] Aborted: no valid target.");
            yield break;
        }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attacker.attackRange)
        {
            if (debug) Debug.Log($"[EchoThorns] Aborted: {target.name} out of range (dist={dist:F2} > range={attacker.attackRange:F2}).");
            yield break;
        }

        int thornsBonus = 0;
        var status = target.GetComponent<ThornsStatus>();
        if (status != null)
        {
            int rawBonus = status.DamagePerThorn * status.ThornAmount;
            thornsBonus = Mathf.RoundToInt(rawBonus * thornsBonusMultiplier);

            if (debug)
                Debug.Log($"[EchoThorns] Target {target.name}: dpt={status.DamagePerThorn}, stacks={status.ThornAmount}, " +
                          $"rawBonus={rawBonus}, scaledBonus={thornsBonus} (x{thornsBonusMultiplier})");
        }
        else if (debug)
        {
            Debug.Log($"[EchoThorns] Target {target.name} has NO ThornsStatus component — thorns bonus = 0.");
        }

        if (maxThornsBonus > 0 && thornsBonus > maxThornsBonus)
        {
            if (debug)
                Debug.Log($"[EchoThorns] Thorns bonus capped: {thornsBonus} -> {maxThornsBonus}");
            thornsBonus = maxThornsBonus;
        }

        int baseDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.attackDamage * baseMultiplier));
        int totalDamage = baseDamage + thornsBonus;

        if (debug)
            Debug.Log($"[EchoThorns] Firing echo at {target.name}: base={baseDamage}, thornsBonus={thornsBonus}, TOTAL={totalDamage}, type={attacker.attackType}");

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
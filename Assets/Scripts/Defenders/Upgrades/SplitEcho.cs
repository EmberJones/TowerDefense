using UnityEngine;

/// <summary>
/// LM Combo: Echo Attack (DoubleShot) can trigger on every SplitShot extra target.
/// </summary>
public class SplitEcho : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float extraEchoChance = 0.5f;

    [SerializeField] private float echoDelay = 0.1f;

    [SerializeField] private float echoDamageMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Attacker attacker;
    private SplitShot splitShot;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        splitShot = GetComponentInParent<SplitShot>();

        if (splitShot != null)
            splitShot.OnExtraShotFired += HandleExtraHit;

        if (debug)
            Debug.Log($"[SplitEcho] Awake on '{name}', attacker={(attacker != null ? attacker.name : "NULL")}, " +
                      $"splitShot={(splitShot != null ? splitShot.name : "NULL")}, chance={extraEchoChance}");
    }

    private void OnDestroy()
    {
        if (splitShot != null)
            splitShot.OnExtraShotFired -= HandleExtraHit;
    }

    private void HandleExtraHit(Transform target)
    {
        if (target == null) return;

        float roll = Random.value;
        bool proc = roll <= extraEchoChance;

        if (debug)
            Debug.Log($"[SplitEcho] OnExtraShotFired target={target.name}, roll={roll:F3} vs chance={extraEchoChance:F3} => {(proc ? "PROC" : "no proc")}");

        if (!proc) return;

        StartCoroutine(FireEcho(target));
    }

    private System.Collections.IEnumerator FireEcho(Transform target)
    {
        yield return null;
        yield return new WaitForSeconds(echoDelay);

        if (target == null)
        {
            if (debug) Debug.Log("[SplitEcho] Aborted: target destroyed before echo fired.");
            yield break;
        }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attacker.attackRange)
        {
            if (debug) Debug.Log($"[SplitEcho] Aborted: {target.name} out of range (dist={dist:F2} > range={attacker.attackRange:F2}).");
            yield break;
        }

        int originalDamage = attacker.attackDamage;
        int scaled = Mathf.Max(1, Mathf.RoundToInt(originalDamage * echoDamageMultiplier));

        if (debug)
            Debug.Log($"[SplitEcho] Firing echo at {target.name}: originalDmg={originalDamage}, scaledDmg={scaled}, type={attacker.attackType}");

        attacker.attackDamage = scaled;

        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            var dmg = target.GetComponent<IDamageable>();
            dmg?.TakeDamage(scaled);
            attacker.SendMessage("HandleHit", target, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            attacker.SendMessage("FireRanged", target, SendMessageOptions.DontRequireReceiver);
        }

        attacker.attackDamage = originalDamage;
    }
}
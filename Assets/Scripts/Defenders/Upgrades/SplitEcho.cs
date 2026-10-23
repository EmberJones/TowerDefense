using UnityEngine;

/// <summary>
/// LM Combo: Echo Attack (DoubleShot) can trigger on every SplitShot extra target.
/// Requires both SplitShot and DoubleShot to be present on the tower
/// (via Left + Middle upgrades), OR can be paired with its own copies.
/// </summary>
public class SplitEcho : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float extraEchoChance = 0.5f;

    [SerializeField] private float echoDelay = 0.1f;

    [SerializeField] private float echoDamageMultiplier = 1f;

    private Attacker attacker;
    private SplitShot splitShot;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        splitShot = GetComponentInParent<SplitShot>();
        if (splitShot != null)
            splitShot.OnExtraShotFired += HandleExtraHit;
    }

    private void OnDestroy()
    {
        if (splitShot != null)
            splitShot.OnExtraShotFired -= HandleExtraHit;
    }

    private void HandleExtraHit(Transform target)
    {
        if (target == null) return;
        if (Random.value > extraEchoChance) return;
        StartCoroutine(FireEcho(target));
    }

    private System.Collections.IEnumerator FireEcho(Transform target)
    {
        yield return null;
        yield return new WaitForSeconds(echoDelay);

        if (target == null) yield break;
        if (Vector3.Distance(transform.position, target.position) > attacker.attackRange) yield break;

        int originalDamage = attacker.attackDamage;
        int scaled = Mathf.Max(1, Mathf.RoundToInt(originalDamage * echoDamageMultiplier));
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
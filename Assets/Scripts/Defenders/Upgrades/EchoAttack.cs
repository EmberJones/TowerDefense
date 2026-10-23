using UnityEngine;

public class EchoAttack : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float procChance = 0.5f;

    [SerializeField] private float delay = 0.1f;

    [SerializeField] private float damageMultiplier = 1f;

    [SerializeField] private bool retargetIfDead = true;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Attacker attacker;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttack += HandleAttack;

        if (debug)
            Debug.Log($"[EchoAttack] Awake on '{name}', attacker={(attacker != null ? attacker.name : "NULL")}, procChance={procChance}, delay={delay}, dmgMult={damageMultiplier}");
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttack -= HandleAttack;
    }

    private void HandleAttack(Transform target)
    {
        if (target == null) return;

        float roll = Random.value;
        bool proc = roll <= procChance;

        if (debug)
            Debug.Log($"[EchoAttack] OnAttack target={target.name}, roll={roll:F3} vs procChance={procChance:F3} => {(proc ? "PROC" : "no proc")}");

        if (!proc) return;

        StartCoroutine(FireFollowUp(target));
    }

    private System.Collections.IEnumerator FireFollowUp(Transform originalTarget)
    {
        yield return null;
        yield return new WaitForSeconds(delay);

        Transform target = originalTarget;
        if (target == null && retargetIfDead)
        {
            target = attacker.CurrentTarget;
            if (debug)
                Debug.Log($"[EchoAttack] Original target dead/null, retargeted to {(target != null ? target.name : "NULL")}");
        }

        if (target == null)
        {
            if (debug) Debug.Log("[EchoAttack] Follow-up aborted: no valid target.");
            yield break;
        }

        int originalDamage = attacker.attackDamage;
        int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(originalDamage * damageMultiplier));

        if (debug)
            Debug.Log($"[EchoAttack] Follow-up firing at {target.name}: originalDmg={originalDamage}, scaledDmg={scaledDamage}, type={attacker.attackType}");

        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            attacker.attackDamage = scaledDamage;
            var dmg = target.GetComponent<IDamageable>();
            dmg?.TakeDamage(scaledDamage);
            attacker.SendMessage("HandleHit", target, SendMessageOptions.DontRequireReceiver);
            attacker.attackDamage = originalDamage;

            if (debug)
                Debug.Log($"[EchoAttack] Melee follow-up applied {scaledDamage} to {target.name} (damageable={(dmg != null ? "yes" : "NO")})");
        }
        else
        {
            attacker.attackDamage = scaledDamage;
            attacker.SendMessage("FireRanged", target, SendMessageOptions.DontRequireReceiver);
            attacker.attackDamage = originalDamage;

            if (debug)
                Debug.Log($"[EchoAttack] Ranged follow-up fired at {target.name} with dmg={scaledDamage}");
        }
    }
}
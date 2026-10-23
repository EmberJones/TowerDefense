using UnityEngine;

public class EchoAttack : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float procChance = 0.5f;

    [SerializeField] private float delay = 0.1f;

    [SerializeField] private float damageMultiplier = 1f;

    [SerializeField] private bool retargetIfDead = true;

    private Attacker attacker;

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

    private void HandleAttack(Transform target)
    {
        if (target == null) return;
        if (Random.value > procChance) return;

        StartCoroutine(FireFollowUp(target));
    }

    private System.Collections.IEnumerator FireFollowUp(Transform originalTarget)
    {
        yield return null;
        yield return new WaitForSeconds(delay);

        Transform target = originalTarget;
        if (target == null && retargetIfDead)
            target = attacker.CurrentTarget;

        if (target == null) yield break;

        int originalDamage = attacker.attackDamage;
        int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(originalDamage * damageMultiplier));

        if (attacker.attackType == Attacker.AttackType.Melee)
        {
            // Temporarily swap damage, deal, then restore so we don't mutate inspector state.
            attacker.attackDamage = scaledDamage;
            var dmg = target.GetComponent<IDamageable>();
            dmg?.TakeDamage(scaledDamage);
            attacker.SendMessage("HandleHit", target, SendMessageOptions.DontRequireReceiver);
            attacker.attackDamage = originalDamage;
        }
        else
        {
            attacker.attackDamage = scaledDamage;
            attacker.SendMessage("FireRanged", target, SendMessageOptions.DontRequireReceiver);
            attacker.attackDamage = originalDamage;
        }
    }
}
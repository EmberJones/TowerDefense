using UnityEngine;

public class Thorns : MonoBehaviour
{
    public int damagePerThorn = 1;
    public int thornAmount = 1;
    public bool suppressNormalApplication = false;

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
        if (suppressNormalApplication) return;

        ThornsStatus thorns = target.GetComponent<ThornsStatus>();
        if (thorns == null)
            thorns = target.gameObject.AddComponent<ThornsStatus>();

        thorns.ApplyThorns(damagePerThorn, thornAmount);
    }
}
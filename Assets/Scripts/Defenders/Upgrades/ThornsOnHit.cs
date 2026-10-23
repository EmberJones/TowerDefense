using UnityEngine;

public class Thorns : MonoBehaviour
{
    public int damagePerThorn = 1;
    public int thornAmount = 1;
    public bool suppressNormalApplication = false;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Attacker attacker;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttackHit += HandleHit;

        if (debug)
            Debug.Log($"[Thorns] Awake on '{name}', attacker={(attacker != null ? attacker.name : "NULL")}, dpt={damagePerThorn}, amount={thornAmount}, suppressed={suppressNormalApplication}");
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttackHit -= HandleHit;
    }

    private void HandleHit(Transform target)
    {
        if (target == null) return;

        if (suppressNormalApplication)
        {
            if (debug)
                Debug.Log($"[Thorns] Suppressed per-hit application on {target.name} (LR combo active).");
            return;
        }

        ThornsStatus thorns = target.GetComponent<ThornsStatus>();
        bool created = false;
        if (thorns == null)
        {
            thorns = target.gameObject.AddComponent<ThornsStatus>();
            created = true;
        }

        if (debug)
            Debug.Log($"[Thorns] Applying to {target.name}: dpt={damagePerThorn}, amount={thornAmount} (status {(created ? "CREATED" : "existing")})");

        thorns.ApplyThorns(damagePerThorn, thornAmount);
    }
}
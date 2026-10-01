using UnityEngine;

public class BurnOnHitEffect : MonoBehaviour
{
    public int burnDamagePerTick = 3;
    public float tickInterval = 1f;
    public float burnDuration = 4f;

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

        BurnStatus burn = target.GetComponent<BurnStatus>();
        if (burn == null)
            burn = target.gameObject.AddComponent<BurnStatus>();

        burn.ApplyBurn(burnDamagePerTick, tickInterval, burnDuration);
    }
}
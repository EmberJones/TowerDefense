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
        QuickVFX.Burst(QuickVFX.Center(target), new Color(1f, 0.45f, 0.1f), 26, 5f, 0.22f, 0.45f);
        QuickVFX.Ring(target.position, 1.3f, new Color(1f, 0.5f, 0.1f), 0.3f);
        if (target == null) return;

        BurnStatus burn = target.GetComponent<BurnStatus>();
        if (burn == null)
            burn = target.gameObject.AddComponent<BurnStatus>();

        burn.ApplyBurn(burnDamagePerTick, tickInterval, burnDuration);
    }
}
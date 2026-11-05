using UnityEngine;

public class SlowOnHitEffect : MonoBehaviour
{
    [Range(0f, 0.8f)] public float slowFraction = 0.3f;
    public float duration = 2f;

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

        SlowStatus.Apply(target.gameObject, this, slowFraction, duration);
        QuickVFX.Burst(QuickVFX.Center(target), new Color(0.4f, 0.8f, 1f), 8, 2f, 0.12f, 0.4f);
    }
}
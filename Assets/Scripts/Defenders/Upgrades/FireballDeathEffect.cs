using UnityEngine;

public class FireballDeathComboEffect : MonoBehaviour
{
    public float explosionRadius = 3f;
    public int burnDamagePerTick = 3;
    public float tickInterval = 1f;
    public float burnDuration = 4f;
    public LayerMask targetLayer;

    private DeathCurseEffect deathCurse;

    private void Awake()
    {
        deathCurse = transform.parent != null
            ? transform.parent.GetComponentInChildren<DeathCurseEffect>()
            : null;

        if (deathCurse != null)
            deathCurse.OnCurseExplosion += HandleCurseExplosion;
    }

    private void OnDestroy()
    {
        if (deathCurse != null)
            deathCurse.OnCurseExplosion -= HandleCurseExplosion;
    }

    private void HandleCurseExplosion(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            BurnStatus burn = hit.GetComponent<BurnStatus>();
            if (burn == null)
                burn = hit.gameObject.AddComponent<BurnStatus>();

            burn.ApplyBurn(burnDamagePerTick, tickInterval, burnDuration);
        }
    }
}
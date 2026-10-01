using UnityEngine;

public class Maelstrom : MonoBehaviour
{
    private float radius;
    private int damagePerTick;
    private float tickInterval;
    private float remaining;
    private LayerMask targetLayer;
    private float tickTimer;

    public void Initialize(float radius, int damagePerTick, float tickInterval, float duration, LayerMask targetLayer)
    {
        this.radius = radius;
        this.damagePerTick = damagePerTick;
        this.tickInterval = tickInterval;
        this.targetLayer = targetLayer;
        remaining = duration;
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayer);
            foreach (var hit in hits)
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(damagePerTick);
            }
            tickTimer = tickInterval;
        }

        if (remaining <= 0f)
            Destroy(gameObject);
    }
}
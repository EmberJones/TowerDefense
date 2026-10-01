using UnityEngine;

public class BurnStatus : MonoBehaviour
{
    private int damagePerTick;
    private float tickInterval;
    private float remainingDuration;
    private float tickTimer;
    private IDamageable damageable;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    public void ApplyBurn(int damagePerTick, float tickInterval, float duration)
    {
        this.damagePerTick = Mathf.Max(this.damagePerTick, damagePerTick);
        this.tickInterval = tickInterval;
        remainingDuration = Mathf.Max(remainingDuration, duration);
    }

    private void Update()
    {
        if (remainingDuration <= 0f) return;

        remainingDuration -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            damageable?.TakeDamage(damagePerTick);
            tickTimer = tickInterval;
        }

        if (remainingDuration <= 0f)
        {
            Destroy(this);
        }
    }
}
using UnityEngine;

public static class LowHealth
{
    public static float Factor(Health health, float startsAt, float maxedAt)
    {
        if (health == null || health.MaxHealth <= 0 || health.CurrentHealth <= 0) return 0f;
        float fraction = (float)health.CurrentHealth / health.MaxHealth;
        return Mathf.Clamp01(Mathf.InverseLerp(startsAt, maxedAt, fraction));
    }
}

public abstract class LowHealthEffect : MonoBehaviour
{
    [Range(0f, 1f)] public float startsAtHealth = 0.9f;
    [Range(0f, 1f)] public float maxedAtHealth = 0.1f;

    protected Attacker attacker;
    protected Health health;

    protected virtual void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        health = GetComponentInParent<Health>();

        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;
    }

    protected virtual void Start()
    {
        Refresh();
    }

    protected virtual void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        Refresh();
    }

    protected void Refresh()
    {
        Apply(LowHealth.Factor(health, startsAtHealth, maxedAtHealth));
    }

    protected abstract void Apply(float factor);
}
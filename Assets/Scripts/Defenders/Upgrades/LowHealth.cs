using UnityEngine;

public static class LowHealth
{
    public static float Factor(Health health, float threshold)
    {
        if (health == null || health.MaxHealth <= 0 || health.CurrentHealth <= 0) return 0f;

        float fraction = (float)health.CurrentHealth / health.MaxHealth;
        return Mathf.Clamp01((threshold - fraction) / Mathf.Max(threshold, 0.01f));
    }
}

public abstract class LowHealthEffect : MonoBehaviour
{
    [Range(0.05f, 1f)] public float lowHealthThreshold = 0.5f;

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
        Apply(LowHealth.Factor(health, lowHealthThreshold));
    }

    protected abstract void Apply(float factor);
}
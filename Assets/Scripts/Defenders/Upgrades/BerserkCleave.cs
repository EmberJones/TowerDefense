using UnityEngine;

public class BerserkCleave : LowHealthEffect
{
    [Tooltip("Extra cleave radius at 0 HP. 0.75 = +75%.")]
    public float maxRadiusBonus = 0.75f;
    [Tooltip("Extra cleave damage at 0 HP. 0.75 = +75%.")]
    public float maxDamageBonus = 0.75f;

    private CleaveOnHitEffect cleave;

    protected override void Awake()
    {
        base.Awake();

        if (attacker != null)
            cleave = attacker.GetComponentInChildren<CleaveOnHitEffect>();
    }

    protected override void Apply(float factor)
    {
        if (cleave == null) return;

        cleave.radiusScale = 1f + maxRadiusBonus * factor;
        cleave.damageScale = 1f + maxDamageBonus * factor;
    }
}
using UnityEngine;

public class BerserkCrit : LowHealthEffect
{
    [Tooltip("Added to the crit multiplier at 0 HP.")]
    public float maxBonusMultiplier = 2f;

    private Crit crit;

    protected override void Awake()
    {
        base.Awake();

        if (attacker != null)
            crit = attacker.GetComponentInChildren<Crit>();
    }

    protected override void Apply(float factor)
    {
        if (crit != null)
            crit.bonusMultiplier = maxBonusMultiplier * factor;
    }
}
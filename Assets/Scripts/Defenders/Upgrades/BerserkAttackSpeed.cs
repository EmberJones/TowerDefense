using UnityEngine;

public class BerserkAttackSpeed : LowHealthEffect
{
    [Tooltip("Attack speed bonus at 0 HP. 1 = +100%.")]
    public float maxAttackSpeedBonus = 1f;
    public bool scaleAnimationSpeed = true;

    private float appliedMultiplier = 1f;
    private Animator animator;
    private float baseAnimatorSpeed = 1f;

    protected override void Awake()
    {
        base.Awake();

        if (attacker != null)
            animator = attacker.GetComponentInChildren<Animator>();
        if (animator != null)
            baseAnimatorSpeed = animator.speed;
    }

    protected override void Apply(float factor)
    {
        if (attacker == null) return;

        float newMultiplier = 1f + maxAttackSpeedBonus * factor;
        attacker.attackInterval *= appliedMultiplier / newMultiplier;

        if (scaleAnimationSpeed && animator != null)
            animator.speed = baseAnimatorSpeed * newMultiplier;

        appliedMultiplier = newMultiplier;
    }
}
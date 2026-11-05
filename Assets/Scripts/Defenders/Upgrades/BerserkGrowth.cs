using UnityEngine;

public class BerserkGrowth : LowHealthEffect
{
    public float maxScaleBonus = 0.5f;
    public float growSpeed = 2f;
    public Transform target;

    private Vector3 baseScale;
    private float goal;
    private float current;

    protected override void Awake()
    {
        base.Awake();

        if (target == null && attacker != null)
        {
            Animator animator = attacker.GetComponentInChildren<Animator>();
            if (animator != null) target = animator.transform;
        }

        if (target != null) baseScale = target.localScale;
    }

    protected override void Apply(float factor)
    {
        goal = factor;
    }

    private void Update()
    {
        if (target == null) return;
        current = Mathf.MoveTowards(current, goal, growSpeed * Time.deltaTime);
        target.localScale = baseScale * (1f + maxScaleBonus * current);
    }
}
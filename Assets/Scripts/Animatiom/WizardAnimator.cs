using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WizardAnimator : MonoBehaviour
{
    [SerializeField] private Attacker attacker;
    [SerializeField] private Health health;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (attacker != null)
            attacker.OnAttack += HandleAttack;

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttack -= HandleAttack;

        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void HandleAttack()
    {
        animator.SetTrigger(AttackTrigger);
    }

    private void HandleDeath()
    {
        animator.SetTrigger(DieTrigger);

        if (attacker != null)
            attacker.enabled = false;
    }
}
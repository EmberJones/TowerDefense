using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WizardAnimator : MonoBehaviour
{
    [SerializeField] private Attacker attacker;
    [SerializeField] private Health health;
    [SerializeField] private float rotationSpeed = 10f;

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

    private void Update()
    {
        FaceCurrentTarget();
    }

    private void FaceCurrentTarget()
    {
        if (attacker == null || attacker.CurrentTarget == null)
            return;

        Vector3 direction = attacker.CurrentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleAttack(Transform target)
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
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VampireAnimation : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private Health health;
    [SerializeField] private float rotationSpeed = 10f;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Auto-wire if not assigned in the Inspector
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (health == null) health = GetComponent<Health>();
    }

    private void Start()
    {
        if (enemy != null)
            enemy.OnAttack += HandleAttack;

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (enemy != null)
            enemy.OnAttack -= HandleAttack;

        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        FaceCurrentTarget();
    }
    private void FaceCurrentTarget()
    {
        if (enemy == null || enemy.CurrentTarget == null) return;

        Vector3 dir = enemy.CurrentTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void HandleAttack(Transform target)
    {
        animator.SetTrigger(AttackTrigger);
    }

    private void HandleDeath()
    {
        animator.SetTrigger(DieTrigger);

        if (enemy != null)
            enemy.enabled = false;
    }
}
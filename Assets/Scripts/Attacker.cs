using System;
using UnityEngine;

public class Attacker : MonoBehaviour
{
    public float attackRange = 8f;
    public float attackInterval = 1f;
    public int attackDamage = 10;
    public LayerMask targetLayer;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    public event Action OnAttack;

    private float attackTimer;
    private Transform currentTarget;

    private void Update()
    {
        FindTarget();

        if (currentTarget == null)
            return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            Attack(currentTarget);
            attackTimer = attackInterval;
        }
    }

    private void FindTarget()
    {
        if (currentTarget != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            if (d > attackRange)
                currentTarget = null;
            else
                return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayer);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = hit.transform;
            }
        }

        currentTarget = closest;
    }

    private void Attack(Transform target)
    {
        OnAttack?.Invoke();

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        if (projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, attackDamage, projectileSpeed);
        }
        else
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(attackDamage);
        }
    }
}
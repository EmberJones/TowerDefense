using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float attackInterval = 1f;
    public int attackDamage = 10;
    public LayerMask targetLayer;
    public float heightOffset = 0.5f;

    public bool ignoreDefenders = false;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    public event Action OnReachedMainTower;
    public event Action<Transform> OnAttack;

    private Collider currentTargetCollider;
    private List<Vector3> waypoints;
    private int waypointIndex;
    private TerrainGenerator terrainGenerator;
    private Health health;
    private Transform currentTarget;

    public Transform CurrentTarget => currentTarget;

    private float attackTimer;
    private bool reachedTowerReported;

    public void Initialize(List<Vector3> path, TerrainGenerator terrain)
    {
        waypoints = path;
        terrainGenerator = terrain;
        waypointIndex = 0;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (currentTarget != null)
        {
            AttackTarget();
            return;
        }

        FindTargetInRange();
        if (currentTarget != null)
            return;

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypointIndex >= waypoints.Count)
            return;

        Vector3 targetPoint = waypoints[waypointIndex];
        Vector3 dir = targetPoint - transform.position;
        dir.y = 0f;

        float step = moveSpeed * Time.deltaTime;

        if (dir.magnitude <= step)
        {
            waypointIndex++;
        }
        else
        {
            transform.position += dir.normalized * step;
            transform.forward = dir.normalized;
        }

        SnapToTerrain();
    }

    private void SnapToTerrain()
    {
        if (terrainGenerator == null) return;

        float y = terrainGenerator.SampleHeight(transform.position.x, transform.position.z) + heightOffset;
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }

    private void FindTargetInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayer);
        float closestDist = float.MaxValue;
        Transform closest = null;
        Collider closestCollider = null;

        foreach (var hit in hits)
        {
            if (ignoreDefenders && hit.GetComponent<Defender>() != null)
                continue;

            Vector3 closestPoint = hit.ClosestPoint(transform.position);
            float d = Vector3.Distance(transform.position, closestPoint);

            if (d < closestDist)
            {
                closestDist = d;
                closest = hit.transform;
                closestCollider = hit;
            }
        }

        currentTarget = closest;
        currentTargetCollider = closestCollider;
    }

    public void AttackTarget()
    {
        if (currentTarget == null)
        {
            ClearTarget();
            return;
        }

        if (!currentTarget.gameObject.activeInHierarchy
            || currentTargetCollider == null
            || !currentTargetCollider.enabled)
        {
            ClearTarget();
            return;
        }

        Vector3 closestPoint = currentTargetCollider.ClosestPoint(transform.position);
        float d = Vector3.Distance(transform.position, closestPoint);
        if (d > attackRange)
        {
            ClearTarget();
            return;
        }

        if (!reachedTowerReported && currentTarget.GetComponent<MainTower>() != null)
        {
            reachedTowerReported = true;
            OnReachedMainTower?.Invoke();
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            PerformAttack(currentTarget);
            attackTimer = attackInterval;
        }
    }

    private void ClearTarget()
    {
        currentTarget = null;
        currentTargetCollider = null;
    }

    private void PerformAttack(Transform target)
    {
        OnAttack?.Invoke(target);

        if (projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, transform.rotation);
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

    private void HandleDeath()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
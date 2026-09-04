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

    private List<Vector3> waypoints;
    private int waypointIndex;
    private TerrainGenerator terrainGenerator;
    private Health health;
    private Transform currentTarget;
    private float attackTimer;

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

    private void AttackTarget()
    {
        if (currentTarget == null)
            return;

        float d = Vector3.Distance(transform.position, currentTarget.position);
        if (d > attackRange)
        {
            currentTarget = null;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            IDamageable damageable = currentTarget.GetComponent<IDamageable>();
            damageable?.TakeDamage(attackDamage);
            attackTimer = attackInterval;
        }
    }

    private void HandleDeath()
    {
        Destroy(gameObject);
    }
}
using System;
using UnityEngine;

public class Attacker : MonoBehaviour
{
    public enum AttackType { Ranged, Melee }

    [SerializeField] public AttackType attackType = AttackType.Ranged;
    [SerializeField] public float attackRange = 8f;
    [SerializeField] public float attackInterval = 1f;
    [SerializeField] public int attackDamage = 10;
    [SerializeField] public LayerMask targetLayer;
    [SerializeField] public GameObject projectilePrefab;
    [SerializeField] public Transform firePoint;
    [SerializeField] public float projectileSpeed = 20f;
    [SerializeField] public bool deferRangedResolution = false;

    [Header("Range Indicator")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private Color rangeColor = new Color(1f, 0.85f, 0f, 0.35f);
    [SerializeField] private float rangeLineWidth = 0.05f;
    [SerializeField] private int rangeSegments = 64;

    public LineRenderer rangeIndicator;

    public event Action<Transform> OnAttack;
    public event Action<Transform> OnAttackHit;

    private float attackTimer;
    private Transform currentTarget;
    private Transform pendingTarget;

    public Transform CurrentTarget => currentTarget;

    private void Awake()
    {
        CreateRangeIndicator();
    }

    private void Update()
    {
        UpdateRangeIndicator();
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
        OnAttack?.Invoke(target);

        if (attackType == AttackType.Melee || deferRangedResolution)
        {
            pendingTarget = target;
            return;
        }

        FireRanged(target);
    }

    // Call this from an Animation Event at the exact frame the hit/release should land.
    public void ResolveAttack()
    {
        if (pendingTarget == null) return;

        Transform target = pendingTarget;
        pendingTarget = null;

        float d = Vector3.Distance(transform.position, target.position);
        if (d > attackRange) return;

        if (attackType == AttackType.Melee)
            ApplyDamage(target);
        else
            FireRanged(target);
    }

    private void FireRanged(Transform target)
    {
        if (projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, attackDamage, projectileSpeed, HandleHit);
        }
        else
        {
            ApplyDamage(target);
        }
    }

    private void ApplyDamage(Transform target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(attackDamage);
        HandleHit(target);
    }

    private void HandleHit(Transform target)
    {
        OnAttackHit?.Invoke(target);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void CreateRangeIndicator()
    {
        GameObject indicatorObject = new GameObject("Range Indicator");
        indicatorObject.transform.SetParent(transform);
        indicatorObject.transform.localPosition = Vector3.zero;
        indicatorObject.transform.localRotation = Quaternion.identity;

        rangeIndicator = indicatorObject.AddComponent<LineRenderer>();
        rangeIndicator.useWorldSpace = false;
        rangeIndicator.loop = true;
        rangeIndicator.positionCount = rangeSegments;
        rangeIndicator.startWidth = rangeLineWidth;
        rangeIndicator.endWidth = rangeLineWidth;

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = rangeColor;
        rangeIndicator.material = material;

        DrawRangeCircle();
    }

    private void UpdateRangeIndicator()
    {
        if (rangeIndicator == null) return;

        rangeIndicator.enabled = showRangeIndicator;
        if (showRangeIndicator)
            DrawRangeCircle();
    }

    private void DrawRangeCircle()
    {
        if (rangeIndicator == null) return;

        rangeIndicator.positionCount = rangeSegments;
        for (int i = 0; i < rangeSegments; i++)
        {
            float angle = ((float)i / rangeSegments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * attackRange;
            float z = Mathf.Sin(angle) * attackRange;
            rangeIndicator.SetPosition(i, new Vector3(x, 0.05f, z));
        }
    }
}

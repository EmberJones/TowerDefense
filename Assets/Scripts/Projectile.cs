using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private Collider targetCollider;
    private int damage;
    private float speed;
    private Action<Transform> onHit;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    public void Initialize(Transform target, int damage, float speed, Action<Transform> onHit = null)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.onHit = onHit;

        if (target != null)
            targetCollider = target.GetComponentInChildren<Collider>();
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 aimPoint = GetAimPoint();
        Vector3 dir = aimPoint - transform.position;
        float step = speed * Time.deltaTime;

        bool insideTarget = targetCollider != null
                            && targetCollider.enabled
                            && targetCollider.bounds.Contains(transform.position);

        if (dir.magnitude <= step || insideTarget)
        {
            HitTarget();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, aimPoint, step);
        transform.forward = dir.normalized;
    }

    private Vector3 GetAimPoint()
    {
        if (targetCollider != null && targetCollider.enabled)
            return targetCollider.bounds.center;

        return target.position + Vector3.up;
    }

    private void HitTarget()
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage);
        onHit?.Invoke(target);
        Destroy(gameObject);
    }
}
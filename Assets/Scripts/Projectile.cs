using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private int damage;
    private float speed;

    public void Initialize(Transform target, int damage, float speed)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.position + Vector3.up;
        Vector3 dir = targetPos - transform.position;
        float step = speed * Time.deltaTime;

        if (dir.magnitude <= step)
        {
            HitTarget();
            return;
        }

        transform.position += dir.normalized * step;
        transform.forward = dir.normalized;
    }

    private void HitTarget()
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
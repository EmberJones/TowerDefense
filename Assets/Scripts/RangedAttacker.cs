using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RangedAttacker : MonoBehaviour
{
    [SerializeField] private int Damage = 10;
    [SerializeField] public float AttackRange = 10f;
    [SerializeField] private float AttackSpeed = 1.5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField]private List<Enemy> enemiesInRange = new List<Enemy>();
    private float AttackCooldown=0;
    SphereCollider sphereCollider;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.radius = AttackRange;
        }
    }

    void Update()
    {
        if (enemiesInRange.Count > 0 && AttackCooldown <= 0)
        {
            Attack();
        }
        AttackCooldown -= Time.deltaTime;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
                enemiesInRange.Add(enemy);
        }
    }

    void Attack()
    {

        GameObject projectile = Instantiate<GameObject>(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<Projectile>().damage = Damage;
        projectile.GetComponent<Projectile>().speed = projectileSpeed;
        projectile.GetComponent<Projectile>().lifetime = AttackRange / projectileSpeed;
        AttackCooldown = AttackSpeed;
    }

    public Transform GetClosestObjectLinq(Transform target, List<Transform> objectsToCheck)
    {
        return objectsToCheck?
            .Where(obj => obj != null)
            .OrderBy(obj => (obj.position - target.position).sqrMagnitude)
            .FirstOrDefault();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, AttackRange);
    }
}

using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] public float speed = 20f;
    [SerializeField] public float lifetime = 3f;
    [SerializeField] public int damage = 5;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider collision)
    {
        // Replace with your target's health system logic
        if (collision.gameObject.CompareTag("Enemy")&&collision.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject); 
        }

        if (collision.gameObject.CompareTag("Terrain"))
        {
            Destroy(gameObject);
        }
    }
}

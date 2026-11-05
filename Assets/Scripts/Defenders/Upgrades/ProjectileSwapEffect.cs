using UnityEngine;

public class ProjectileSwapEffect : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 50f;

    private Attacker attacker;
    private GameObject originalPrefab;
    private float originalSpeed;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker == null) return;

        originalPrefab = attacker.projectilePrefab;
        originalSpeed = attacker.projectileSpeed;

        if (projectilePrefab != null)
            attacker.projectilePrefab = projectilePrefab;
        attacker.projectileSpeed = projectileSpeed;
    }

    private void OnDestroy()
    {
        if (attacker == null) return;

        attacker.projectilePrefab = originalPrefab;
        attacker.projectileSpeed = originalSpeed;
    }
}
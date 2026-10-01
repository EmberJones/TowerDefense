using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]
public class Defender : MonoBehaviour
{
    public float respawnDelay = 5f;

    private Health health;
    private Attacker attacker;
    private TowerUpgradeTree upgradeTree;
    private Collider towerCollider;
    private Renderer[] renderers;

    private void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();
        upgradeTree = GetComponent<TowerUpgradeTree>();
        towerCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();

        health.OnDeath += HandleDeath;
    }

    private void OnMouseDown()
    {
        if (upgradeTree != null)
            UpgradeUI.Instance?.Open(upgradeTree);
    }

    private void HandleDeath()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetAlive(false);

        yield return new WaitForSeconds(respawnDelay);

        health.ResetHealth();
        SetAlive(true);
    }

    private void SetAlive(bool alive)
    {
        if (attacker != null)
            attacker.enabled = alive;

        if (towerCollider != null)
            towerCollider.enabled = alive;

        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = alive;
        }
    }
}
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]

public class Defender : MonoBehaviour
{
    public float respawnDelay = 5f;
    [Tooltip("How long the die animation is allowed to play before the model is hidden.")]
    public float deathAnimationTime = 1.5f;
    public Vector3 spawnOffset = Vector3.zero;

    private Health health;
    private Attacker attacker;
    private TowerUpgradeTree upgradeTree;
    private Collider towerCollider;
    private Renderer[] renderers;
    private Animator animator;

    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        health = GetComponent<Health>();
        attacker = GetComponent<Attacker>();
        upgradeTree = GetComponent<TowerUpgradeTree>();
        towerCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        animator = GetComponentInChildren<Animator>();

        health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
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

        if (attacker != null) attacker.enabled = false;
        if (towerCollider != null) towerCollider.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(DieHash);   
        }

        yield return new WaitForSeconds(deathAnimationTime);
        SetRenderers(false);

        yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay - deathAnimationTime));

        ResetAnimator();

        health.ResetHealth();
        SetAlive(true);
    }

    private void ResetAnimator()
    {
        if (animator == null) return;

        animator.ResetTrigger(DieHash);
        animator.ResetTrigger(AttackHash);
        animator.Rebind();      
        animator.Update(0f);    
    }

    private void SetAlive(bool alive)
    {
        if (attacker != null)
            attacker.enabled = alive;

        if (towerCollider != null)
            towerCollider.enabled = alive;

        SetRenderers(alive);
    }

    private void SetRenderers(bool visible)
    {
        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }
}
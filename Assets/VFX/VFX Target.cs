using System.Collections;
using UnityEngine;

/// <summary>
/// Added automatically by VFXManager to anything with a Health component (enemies, defenders, towers).
/// Watches the health value, and when it drops asks VFXManager to play the damage effect.
/// Also does the actual material-swap flash on this object's renderers.
/// </summary>
[DisallowMultipleComponent]
public class VFXTarget : MonoBehaviour
{
    private Health health;
    private float lastHealth;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashRoutine;
    private bool flashing;

    /// <summary>World-space centre of the visible model, used as the particle spawn point.</summary>
    public Vector3 CenterPoint
    {
        get
        {
            if (renderers == null || renderers.Length == 0)
                return transform.position + Vector3.up * 0.5f;

            Bounds b = default;
            bool has = false;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return has ? b.center : transform.position;
        }
    }

    /// <summary>Rough horizontal radius of the model, so particles can start outside it.</summary>
    public float Radius
    {
        get
        {
            if (renderers == null || renderers.Length == 0) return 0.4f;

            Bounds b = default;
            bool has = false;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return has ? Mathf.Max(0.2f, Mathf.Max(b.extents.x, b.extents.z)) : 0.4f;
        }
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        CacheRenderers();
    }

    private void Start()
    {
        if (health != null)
            lastHealth = health.CurrentHealth;
    }

    private void Update()
    {
        if (health == null) return;

        float current = health.CurrentHealth;
        if (current < lastHealth && VFXManager.Instance != null)
            VFXManager.Instance.PlayDamageEffect(this);

        lastHealth = current;
    }

    private void OnDisable()
    {
        // Pooled or disabled mid-flash: make sure the real materials come back.
        if (flashing) RestoreMaterials();
    }

    // Only meshes and sprites are flashed. Line renderers (range indicators)
    // and particle renderers are skipped on purpose.
    private void CacheRenderers()
    {
        var found = new System.Collections.Generic.List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer)
                found.Add(r);
        }
        renderers = found.ToArray();
        originalMaterials = new Material[renderers.Length][];
    }

    public void Flash(Material flashMaterial, float duration)
    {
        if (flashMaterial == null || renderers == null || renderers.Length == 0) return;
        if (!gameObject.activeInHierarchy) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        if (flashing) RestoreMaterials();

        flashRoutine = StartCoroutine(FlashRoutine(flashMaterial, duration));
    }

    private IEnumerator FlashRoutine(Material flashMaterial, float duration)
    {
        flashing = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            originalMaterials[i] = renderers[i].sharedMaterials;

            var swapped = new Material[originalMaterials[i].Length];
            for (int m = 0; m < swapped.Length; m++)
                swapped[m] = flashMaterial;
            renderers[i].sharedMaterials = swapped;
        }

        yield return new WaitForSeconds(duration);

        RestoreMaterials();
        flashRoutine = null;
    }

    private void RestoreMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
                renderers[i].sharedMaterials = originalMaterials[i];
        }
        flashing = false;
    }
}
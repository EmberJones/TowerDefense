using System.Collections;
using UnityEngine;

/// <summary>
/// Added automatically by VFXManager to anything with a Health component (enemies, defenders, towers).
/// - Watches the health value, and when it drops asks VFXManager to play the damage effect.
/// - Watches for a BurnStatus and, while one is attached, gives off rising embers.
/// - Does the actual material-swap flash on this object's renderers.
/// </summary>
[DisallowMultipleComponent]
public class VFXTarget : MonoBehaviour
{
    private Health health;
    private float lastHealth;

    private BurnStatus burn;
    private float emberTimer;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashRoutine;
    private bool flashing;

    /// <summary>True while a BurnStatus is attached to this object.</summary>
    public bool IsBurning => burn != null;

    /// <summary>World-space bounds of the visible model (meshes and sprites only).</summary>
    public Bounds WorldBounds
    {
        get
        {
            if (renderers == null || renderers.Length == 0)
                return new Bounds(transform.position + Vector3.up * 0.5f, Vector3.one * 0.8f);

            Bounds b = default;
            bool has = false;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return has ? b : new Bounds(transform.position, Vector3.one * 0.8f);
        }
    }

    /// <summary>World-space centre of the visible model, used as the particle spawn point.</summary>
    public Vector3 CenterPoint => WorldBounds.center;

    /// <summary>Rough horizontal radius of the model, so particles can start outside it.</summary>
    public float Radius
    {
        get
        {
            Bounds b = WorldBounds;
            return Mathf.Max(0.2f, Mathf.Max(b.extents.x, b.extents.z));
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
        if (VFXManager.Instance == null) return;

        // Damage flash + particles
        if (health != null)
        {
            float current = health.CurrentHealth;
            if (current < lastHealth)
                VFXManager.Instance.PlayDamageEffect(this);
            lastHealth = current;
        }

        // Burning embers
        if (burn == null)
            TryGetComponent(out burn);

        if (burn != null)
        {
            emberTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.1f, VFXManager.Instance.EmberRate);
            while (emberTimer >= interval)
            {
                emberTimer -= interval;
                VFXManager.Instance.PlayEmbers(WorldBounds, 1);
            }
        }
        else
        {
            emberTimer = 0f;
        }
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
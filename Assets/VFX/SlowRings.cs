using System.Collections.Generic;
using UnityEngine;

public class SlowRings
{
    private class Ring
    {
        public GameObject go;
        public MeshRenderer renderer;
        public float age;
    }

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<Ring> active = new List<Ring>();
    private readonly Stack<Ring> pool = new Stack<Ring>();
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private float spawnTimer;

    public bool HasActive => active.Count > 0;

    public void Tick(VFXManager vfx, Bounds bounds, bool slowed, float dt)
    {
        if (slowed)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                Spawn(vfx);
                spawnTimer = vfx.RingInterval;
            }
        }
        else
        {
            spawnTimer = 0f; // stop making new rings; existing ones finish their fade
        }

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Ring ring = active[i];
            ring.age += dt;
            float t = ring.age / Mathf.Max(0.1f, vfx.RingDuration);

            if (t >= 1f)
            {
                ring.go.SetActive(false);
                pool.Push(ring);
                active.RemoveAt(i);
                continue;
            }

            Place(vfx, ring, t, bounds);
        }
    }

    public void Dispose()
    {
        foreach (Ring r in active)
            if (r.go != null) Object.Destroy(r.go);
        foreach (Ring r in pool)
            if (r.go != null) Object.Destroy(r.go);
        active.Clear();
        pool.Clear();
    }

    private void Spawn(VFXManager vfx)
    {
        Ring ring = pool.Count > 0 ? pool.Pop() : CreateRing(vfx);
        ring.age = 0f;
        ring.go.SetActive(true);
        active.Add(ring);
    }

    private Ring CreateRing(VFXManager vfx)
    {
        var go = new GameObject("Slow Ring");
        go.transform.SetParent(vfx.transform, false);

        go.AddComponent<MeshFilter>().sharedMesh = vfx.RingMesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = vfx.RingMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        return new Ring { go = go, renderer = rend };
    }

    private void Place(VFXManager vfx, Ring ring, float t, Bounds b)
    {
        float radius = Mathf.Max(0.2f, Mathf.Max(b.extents.x, b.extents.z));

        // Starts near the head, sinks to just above the ground at the feet.
        float top = b.min.y + b.size.y * 0.9f;
        float bottom = b.min.y + 0.03f;
        float y = Mathf.Lerp(top, bottom, t);

        // Slightly wider than the body, tightening a little as it sinks.
        float diameter = radius * 2f * 1.3f * Mathf.Lerp(1f, 0.75f, t);

        Transform tr = ring.go.transform;
        tr.position = new Vector3(b.center.x, y, b.center.z);
        tr.localScale = new Vector3(diameter, 1f, diameter);

        // Fade in over the first quarter, fade out over the last third.
        float alpha = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((1f - t) / 0.35f);
        Color c = vfx.SlowColor;
        c.a *= alpha;

        block.SetColor(ColorId, c);
        ring.renderer.SetPropertyBlock(block);
    }
}
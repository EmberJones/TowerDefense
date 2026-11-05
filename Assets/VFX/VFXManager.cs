using System.Collections;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Damage Flash")]
    [Tooltip("Optional. Assign an unlit white material if Shader.Find fails in builds.")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Damage Particles")]
    [Tooltip("Optional particle material. Defaults to Sprites/Default.")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color hitParticleColor = Color.white;
    [SerializeField] private int hitParticleCount = 12;
    [SerializeField] private float hitParticleSpeed = 4f;
    [SerializeField] private float hitParticleLifetime = 0.45f;
    [SerializeField] private Vector2 hitParticleSize = new Vector2(0.15f, 0.3f);

    [Header("Burn Effect")]
    [Tooltip("Colour of the flash when a burning target takes damage.")]
    [SerializeField] private Color burnFlashColor = new Color(1f, 0.45f, 0.1f);
    [Tooltip("Each ember picks one of these at random.")]
    [SerializeField]
    private Color[] emberColors =
    {
        new Color(1f, 0.85f, 0.2f),  // yellow
        new Color(1f, 0.5f, 0.1f),   // orange
        new Color(0.9f, 0.2f, 0.1f)  // red
    };
    [Tooltip("Embers per second given off by each burning target.")]
    [SerializeField] private float emberRate = 14f;
    [SerializeField] private float emberRiseSpeed = 1.6f;
    [SerializeField] private float emberLifetime = 0.8f;
    [SerializeField] private Vector2 emberSize = new Vector2(0.12f, 0.25f);
    [Tooltip("Embers released in one puff each time a burning target takes damage.")]
    [SerializeField] private int emberTickBurst = 5;

    [Header("Bleed Effect (Thorns)")]
    [SerializeField] private Color bleedFlashColor = new Color(0.9f, 0.05f, 0.05f);
    [SerializeField] private float bleedFlashDuration = 0.15f;
    [Tooltip("Each blood chunk picks one of these at random.")]
    [SerializeField]
    private Color[] bloodColors =
    {
        new Color(0.75f, 0.05f, 0.05f),
        new Color(0.55f, 0.02f, 0.04f),
        new Color(0.9f, 0.1f, 0.1f)
    };
    [Tooltip("How long a target keeps dripping after a thorn hit (refreshed by each new hit).")]
    [SerializeField] private float bleedDuration = 2f;
    [Tooltip("Drips per second while bleeding.")]
    [SerializeField] private float bloodDripRate = 9f;
    [SerializeField] private float bloodBurstSpeed = 3f;
    [SerializeField] private float bloodGravity = 3f;
    [SerializeField] private Vector2 bloodSize = new Vector2(0.08f, 0.17f);

    [Header("Slow Effect")]
    [Tooltip("Colour of the slow rings, the hit flash and the hit sparks.")]
    [SerializeField] private Color slowColor = new Color(0.08f, 0.18f, 0.7f, 1f);
    [Tooltip("How long one ring takes to appear, sink to the feet and fade out.")]
    [SerializeField] private float ringDuration = 0.9f;
    [Tooltip("Seconds between new rings while an enemy is slowed.")]
    [SerializeField] private float ringInterval = 0.4f;

    [Header("Auto Setup")]
    [Tooltip("Automatically adds a VFXTarget to every object with a Health component.")]
    [SerializeField] private bool autoAttachToHealth = true;
    [SerializeField] private float scanInterval = 0.5f;

    private Material burnFlashMaterial;
    private Material bleedFlashMaterial;
    private Material slowFlashMaterial;
    private Material ringMaterial;
    private Mesh ringMesh;
    private ParticleSystem hitParticles;
    private ParticleSystem emberParticles;
    private ParticleSystem bloodParticles;
    private Mesh lowPolyMesh;

    public Material FlashMaterial => flashMaterial;
    public float FlashDuration => flashDuration;
    public float EmberRate => emberRate;
    public float BleedDuration => bleedDuration;
    public float BloodDripRate => bloodDripRate;
    public Color SlowColor => slowColor;
    public float RingDuration => ringDuration;
    public float RingInterval => ringInterval;
    public Mesh RingMesh => ringMesh;
    public Material RingMaterial => ringMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (flashMaterial == null)
            flashMaterial = CreateFlashMaterial(Color.white);
        burnFlashMaterial = CreateFlashMaterial(burnFlashColor);
        bleedFlashMaterial = CreateFlashMaterial(bleedFlashColor);
        slowFlashMaterial = CreateFlashMaterial(slowColor);

        ringMesh = CreateRingMesh();
        ringMaterial = new Material(Shader.Find("Sprites/Default"));

        lowPolyMesh = CreateLowPolyMesh();
        hitParticles = CreateHitParticleSystem("Hit Particles");
        emberParticles = CreateEmberParticleSystem("Ember Particles");
        bloodParticles = CreateBloodParticleSystem("Blood Particles");
    }

    private void Start()
    {
        if (autoAttachToHealth)
            StartCoroutine(AttachRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- Public API


    public void PlayDamageEffect(VFXTarget target)
    {
        if (target == null) return;

        if (target.IsBurning)
        {
            target.Flash(burnFlashMaterial, flashDuration);
            PlayEmbers(target.WorldBounds, emberTickBurst, 1.8f);
        }
        else if (target.IsSlowed)
        {
            target.Flash(slowFlashMaterial, flashDuration);
            PlayBurst(target.CenterPoint, slowColor, hitParticleCount, target.Radius);
        }
        else
        {
            target.Flash(flashMaterial, flashDuration);
            PlayBurst(target.CenterPoint, hitParticleColor, hitParticleCount, target.Radius);
        }
    }

    public void PlayBleedHit(VFXTarget target, int thornsAdded)
    {
        if (target == null) return;

        target.Flash(bleedFlashMaterial, bleedFlashDuration);
        int count = Mathf.Clamp(6 + thornsAdded * 3, 6, 30);
        PlayBlood(target.WorldBounds, count, bloodBurstSpeed);
    }

    public void PlayBlood(Bounds bounds, int count, float speed)
    {
        if (bloodParticles == null) return;

        float gravity = Mathf.Abs(Physics.gravity.y) * bloodGravity;
        float groundY = bounds.min.y;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y + bounds.size.y * 0.25f, bounds.max.y * 0.95f + bounds.min.y * 0.05f),
                Random.Range(bounds.min.z, bounds.max.z));

            Vector2 flat = Random.insideUnitCircle * Random.Range(speed * 0.3f, speed);
            float vy = Random.Range(-0.2f, speed * 0.5f);
            Vector3 vel = new Vector3(flat.x, vy, flat.y);

            // Time to fall from pos.y to groundY: solve h + vy*t - 0.5*g*t^2 = 0.
            float h = Mathf.Max(pos.y - groundY, 0.05f);
            float fallTime = (vy + Mathf.Sqrt(vy * vy + 2f * gravity * h)) / gravity;

            var emit = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = Random.Range(bloodSize.x, bloodSize.y),
                startLifetime = Mathf.Clamp(fallTime, 0.1f, 3f),
                startColor = PickColor(bloodColors, new Color(0.75f, 0.05f, 0.05f))
            };
            bloodParticles.Emit(emit, 1);
        }
    }

    public void PlayBurst(Vector3 position, Color color, int count, float radius = 0.3f)
    {
        if (hitParticles == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y); // keep sparks above ground level

            var emit = new ParticleSystem.EmitParams
            {
                position = position + dir * radius,
                velocity = dir * Random.Range(hitParticleSpeed * 0.5f, hitParticleSpeed),
                startSize = Random.Range(hitParticleSize.x, hitParticleSize.y),
                startLifetime = hitParticleLifetime,
                startColor = color
            };
            hitParticles.Emit(emit, 1);
        }
    }

    public void PlayEmbers(Bounds bounds, int count, float speedMultiplier = 1f)
    {
        if (emberParticles == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y + bounds.size.y * 0.2f, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            Vector3 vel = new Vector3(
                Random.Range(-0.4f, 0.4f),
                emberRiseSpeed * Random.Range(0.7f, 1.3f) * speedMultiplier,
                Random.Range(-0.4f, 0.4f));

            var emit = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = Random.Range(emberSize.x, emberSize.y),
                startLifetime = emberLifetime * Random.Range(0.8f, 1.2f),
                startColor = PickColor(emberColors, new Color(1f, 0.5f, 0.1f))
            };
            emberParticles.Emit(emit, 1);
        }
    }

    public void PlayTrailEmbers(Vector3 position, int count = 1)
    {
        if (emberParticles == null) return;

        for (int i = 0; i < count; i++)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.08f,
                velocity = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0.2f, 0.8f),
                    Random.Range(-0.3f, 0.3f)),
                startSize = Random.Range(emberSize.x, emberSize.y) * 0.8f,
                startLifetime = emberLifetime * Random.Range(0.5f, 0.8f),
                startColor = PickColor(emberColors, new Color(1f, 0.5f, 0.1f))
            };
            emberParticles.Emit(emit, 1);
        }
    }

    public void PlayTrailBlood(Vector3 position, int count = 1)
    {
        if (bloodParticles == null) return;

        for (int i = 0; i < count; i++)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.06f,
                velocity = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    Random.Range(-0.1f, 0.3f),
                    Random.Range(-0.25f, 0.25f)),
                startSize = Random.Range(bloodSize.x, bloodSize.y),
                startLifetime = Random.Range(0.35f, 0.55f),
                startColor = PickColor(bloodColors, new Color(0.75f, 0.05f, 0.05f))
            };
            bloodParticles.Emit(emit, 1);
        }
    }

    public void PlaySlowApplied(VFXTarget target)
    {
        if (target == null) return;

        target.Flash(slowFlashMaterial, flashDuration);
        PlayBurst(target.CenterPoint, slowColor, hitParticleCount, target.Radius);
    }

    // ---------------------------------------------------------------- Setup

    private static Color PickColor(Color[] palette, Color fallback)
    {
        if (palette == null || palette.Length == 0) return fallback;
        return palette[Random.Range(0, palette.Length)];
    }

    private IEnumerator AttachRoutine()
    {
        var wait = new WaitForSeconds(scanInterval);
        while (true)
        {
            foreach (Health h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                if (h.GetComponent<VFXTarget>() == null)
                    h.gameObject.AddComponent<VFXTarget>();
            }
            yield return wait;
        }
    }

    private Material CreateFlashMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }


    private Mesh CreateLowPolyMesh()
    {
        Vector3 t = new Vector3(0f, 0.6f, 0f);
        Vector3 b = new Vector3(0f, -0.6f, 0f);
        Vector3 px = new Vector3(0.5f, 0f, 0f);
        Vector3 nx = new Vector3(-0.5f, 0f, 0f);
        Vector3 pz = new Vector3(0f, 0f, 0.5f);
        Vector3 nz = new Vector3(0f, 0f, -0.5f);

        Vector3[][] faces =
        {
            new[] { t, pz, px }, new[] { t, px, nz }, new[] { t, nz, nx }, new[] { t, nx, pz },
            new[] { b, px, pz }, new[] { b, nz, px }, new[] { b, nx, nz }, new[] { b, pz, nx }
        };
        float[] shade = { 1f, 0.8f, 0.9f, 0.7f, 0.6f, 0.45f, 0.55f, 0.4f };

        var verts = new Vector3[24];
        var cols = new Color[24];
        var tris = new int[24];
        for (int f = 0; f < 8; f++)
        {
            for (int v = 0; v < 3; v++)
            {
                int i = f * 3 + v;
                verts[i] = faces[f][v];
                cols[i] = new Color(shade[f], shade[f], shade[f], 1f);
                tris[i] = i;
            }
        }

        var mesh = new Mesh { name = "LowPolyChunk" };
        mesh.vertices = verts;
        mesh.colors = cols;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Mesh CreateRingMesh()
    {
        const int segments = 8;
        const float outer = 0.5f;
        const float inner = 0.39f;

        var verts = new Vector3[segments * 2];
        var normals = new Vector3[segments * 2];
        var cols = new Color[segments * 2];
        var tris = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            verts[i * 2] = new Vector3(cos * outer, 0f, sin * outer);
            verts[i * 2 + 1] = new Vector3(cos * inner, 0f, sin * inner);
            normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
            cols[i * 2] = cols[i * 2 + 1] = Color.white;

            int o0 = i * 2, i0 = i * 2 + 1;
            int o1 = ((i + 1) % segments) * 2, i1 = o1 + 1;
            int t = i * 6;
            tris[t] = o0; tris[t + 1] = o1; tris[t + 2] = i0;
            tris[t + 3] = i0; tris[t + 4] = o1; tris[t + 5] = i1;
        }

        var mesh = new Mesh { name = "LowPolyRing" };
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.colors = cols;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;
        return new Material(Shader.Find("Sprites/Default"));
    }

    private ParticleSystem CreateHitParticleSystem(string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = hitParticleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(hitParticleSpeed * 0.5f, hitParticleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(hitParticleSize.x, hitParticleSize.y);
        main.gravityModifier = 0.6f;
        main.maxParticles = 256;

        // Particles are emitted from code, so the timed emitter stays off.
        var emission = ps.emission;
        emission.enabled = false;

        // Position and velocity are set per particle in PlayBurst, so no shape module.
        var shape = ps.shape;
        shape.enabled = false;

        // Shrink and fade over the particle's life.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = fade;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = CreateParticleMaterial();

        ps.Play();
        return ps;
    }

    private ParticleSystem CreateEmberParticleSystem(string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f; // embers rise on their own velocity
        main.maxParticles = 512;

        // Random starting orientation so every chunk looks different.
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        // Chunky tumble, like low-poly debris.
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-4f, 4f);
        rot.y = new ParticleSystem.MinMaxCurve(-4f, 4f);
        rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        // Shrink to nothing.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Cool from the picked colour toward deep red as the ember dies.
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var cool = new Gradient();
        cool.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.35f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = cool;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Mesh;
        rend.mesh = lowPolyMesh;
        rend.sharedMaterial = CreateParticleMaterial();

        ps.Play();
        return ps;
    }

    private ParticleSystem CreateBloodParticleSystem(string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = bloodGravity; // blood falls; lifetime is set per particle to end at the ground
        main.maxParticles = 512;

        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        // Full size for most of the fall, then shrink away so drops don't pop out of existence.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));

        // Slower tumble than embers: heavier droplets.
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
        rot.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
        rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Mesh;
        rend.mesh = lowPolyMesh;
        rend.sharedMaterial = CreateParticleMaterial();

        ps.Play();
        return ps;
    }
}
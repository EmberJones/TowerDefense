using System.Collections;
using UnityEngine;

/// <summary>
/// Central hub for all visual effects. Other scripts call into this (or, for damage,
/// a VFXTarget does it automatically) instead of owning their own effect code.
/// To add a new effect: add a public Play___ method here, plus any settings it needs.
/// </summary>
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

    [Header("Auto Setup")]
    [Tooltip("Automatically adds a VFXTarget to every object with a Health component.")]
    [SerializeField] private bool autoAttachToHealth = true;
    [SerializeField] private float scanInterval = 0.5f;

    private ParticleSystem hitParticles;

    public Material FlashMaterial => flashMaterial;
    public float FlashDuration => flashDuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (flashMaterial == null)
            flashMaterial = CreateFlashMaterial();

        hitParticles = CreateParticleSystem("Hit Particles");
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

    /// <summary>White flash plus a small particle burst. Called when a target takes damage.</summary>
    public void PlayDamageEffect(VFXTarget target)
    {
        if (target == null) return;
        target.Flash(flashMaterial, flashDuration);
        PlayBurst(target.CenterPoint, hitParticleColor, hitParticleCount, target.Radius);
    }

    /// <summary>
    /// Emits a burst of particles around a world position. Each particle starts at the
    /// given radius from the centre (so it begins outside the model, not buried inside it)
    /// and flies outward.
    /// </summary>
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

    // ---------------------------------------------------------------- Setup

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

    private Material CreateFlashMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        return mat;
    }

    private ParticleSystem CreateParticleSystem(string objectName)
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
        rend.sharedMaterial = particleMaterial != null
            ? particleMaterial
            : new Material(Shader.Find("Sprites/Default"));

        ps.Play();
        return ps;
    }
}
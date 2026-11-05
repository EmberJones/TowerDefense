using UnityEngine;

public class ProjectileVFX : MonoBehaviour
{
    public enum Style { ArrowTrail, FireTrail, BloodTrail }

    [SerializeField] private Style style = Style.ArrowTrail;

    [Header("Arrow Trail")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 0.9f, 0.9f);
    [SerializeField] private float lineWidth = 0.06f;
    [Tooltip("How long the line lasts behind the arrow, in seconds. Longer = longer line.")]
    [SerializeField] private float lineTime = 0.18f;
    [Tooltip("Where the line starts, relative to the prefab (e.g. the back of the arrow).")]
    [SerializeField] private Vector3 lineOffset = Vector3.zero;

    [Header("Magic Trail")]
    [Tooltip("Distance between particles along the flight path. Smaller = denser trail.")]
    [SerializeField] private float particleSpacing = 0.2f;
    [SerializeField] private int particlesPerStep = 1;

    private static Material sharedLineMaterial;

    private TrailRenderer trail;
    private Vector3 lastPosition;
    private float distanceCarry;

    private void Awake()
    {
        if (style == Style.ArrowTrail)
            CreateTrail();
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (style == Style.ArrowTrail) return;

        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPosition;
        float dist = delta.magnitude;
        lastPosition = pos;

        var vfx = VFXManager.Instance;
        if (vfx == null || dist < 0.0001f) return;

        // Space particles evenly along the path, so the trail looks the same at any speed.
        Vector3 dir = delta / dist;
        distanceCarry += dist;

        int safety = 30;
        float spacing = Mathf.Max(0.02f, particleSpacing);
        while (distanceCarry >= spacing && safety-- > 0)
        {
            distanceCarry -= spacing;
            Vector3 point = pos - dir * distanceCarry;

            if (style == Style.FireTrail)
                vfx.PlayTrailEmbers(point, particlesPerStep);
            else
                vfx.PlayTrailBlood(point, particlesPerStep);
        }
    }

    private void OnDestroy()
    {
        // The projectile is destroyed the moment it hits, which would make the line vanish
        // instantly. Detach it so it can fade out on its own, then clean it up.
        if (trail == null || !gameObject.scene.isLoaded) return;

        trail.emitting = false;
        trail.transform.SetParent(null);
        Destroy(trail.gameObject, trail.time + 0.1f);
    }

    private void CreateTrail()
    {
        var go = new GameObject("Arrow Trail");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = lineOffset;

        trail = go.AddComponent<TrailRenderer>();
        trail.time = lineTime;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 0;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        // Constant width, thinning to a point at the tail.
        trail.widthMultiplier = lineWidth;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

        // Solid at the arrow, fading to nothing at the tail.
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(lineColor, 0f), new GradientColorKey(lineColor, 1f) },
            new[] { new GradientAlphaKey(lineColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;

        if (sharedLineMaterial == null)
            sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
        trail.sharedMaterial = sharedLineMaterial;
    }
}
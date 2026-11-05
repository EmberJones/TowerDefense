using System;
using UnityEngine;

public static class QuickVFX
{
    private static Material material;
    private static Material Mat
    {
        get
        {
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            return material;
        }
    }

    private class Tween : MonoBehaviour
    {
        public float duration;
        public Action<float> onUpdate;
        private float t;

        private void Update()
        {
            t += Time.deltaTime;
            onUpdate(Mathf.Clamp01(t / duration));
            if (t >= duration) Destroy(gameObject);
        }
    }

    public static Vector3 Center(Transform t)
    {
        Collider c = t.GetComponent<Collider>();
        return c != null ? c.bounds.center : t.position + Vector3.up;
    }

    public static void Burst(Vector3 position, Color color, int count = 18, float speed = 4f, float size = 0.15f, float life = 0.4f)
    {
        GameObject go = new GameObject("VFX_Burst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = life;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = 0.4f;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;

        var shrink = ps.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        go.GetComponent<ParticleSystemRenderer>().material = Mat;

        ps.Emit(count);
        UnityEngine.Object.Destroy(go, life + 1f);
    }

    public static void Ring(Vector3 position, float radius, Color color, float duration = 0.3f)
    {
        GameObject go = new GameObject("VFX_Ring");
        go.transform.position = position + Vector3.up * 0.1f;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 32;
        lr.material = Mat;

        Tween tween = go.AddComponent<Tween>();
        tween.duration = duration;
        tween.onUpdate = t =>
        {
            float r = radius * Mathf.Sqrt(t);
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            Color c = color;
            c.a = color.a * (1f - t);
            lr.startColor = c;
            lr.endColor = c;
            lr.widthMultiplier = Mathf.Lerp(0.2f, 0.03f, t);
        };
    }

    public static void Bolt(Vector3 from, Vector3 to, Color color, float duration = 0.25f, int segments = 8, float jitter = 0.35f)
    {
        GameObject go = new GameObject("VFX_Bolt");

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = segments + 1;
        lr.material = Mat;

        Vector3 dir = (to - from).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        side.Normalize();
        Vector3 up = Vector3.Cross(side, dir);

        Tween tween = go.AddComponent<Tween>();
        tween.duration = duration;
        tween.onUpdate = t =>
        {
            for (int i = 0; i <= segments; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)segments);
                if (i > 0 && i < segments)
                    p += side * UnityEngine.Random.Range(-jitter, jitter) + up * UnityEngine.Random.Range(-jitter, jitter);
                lr.SetPosition(i, p);
            }
            Color c = color;
            c.a = 1f - t;
            lr.startColor = c;
            lr.endColor = c;
            lr.widthMultiplier = Mathf.Lerp(0.18f, 0.04f, t);
        };
    }
}
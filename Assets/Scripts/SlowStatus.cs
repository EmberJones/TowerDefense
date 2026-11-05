using System.Collections.Generic;
using UnityEngine;

public class SlowStatus : MonoBehaviour
{
    public const float MaxSlow = 0.8f;

    private class Entry
    {
        public float slow;
        public float remaining;
    }

    private readonly Dictionary<object, Entry> entries = new Dictionary<object, Entry>();
    private readonly List<object> expired = new List<object>();
    private Enemy enemy;

    public static void Apply(GameObject target, object source, float slowFraction, float duration)
    {
        SlowStatus status = target.GetComponent<SlowStatus>();
        if (status == null) status = target.AddComponent<SlowStatus>();
        status.Set(source, slowFraction, duration);
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Set(object source, float slowFraction, float duration)
    {
        if (!entries.TryGetValue(source, out Entry entry))
        {
            entry = new Entry();
            entries[source] = entry;
        }

        entry.slow = Mathf.Clamp(slowFraction, 0f, MaxSlow);
        entry.remaining = duration;
    }

    private void Update()
    {
        float strongest = 0f;
        expired.Clear();

        foreach (var pair in entries)
        {
            pair.Value.remaining -= Time.deltaTime;
            if (pair.Value.remaining <= 0f) expired.Add(pair.Key);
            else strongest = Mathf.Max(strongest, pair.Value.slow);
        }

        foreach (object key in expired) entries.Remove(key);

        if (enemy != null) enemy.speedMultiplier = 1f - strongest;

        if (entries.Count == 0) Destroy(this);
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.speedMultiplier = 1f;
    }
}
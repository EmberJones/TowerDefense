using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathGenerator : MonoBehaviour
{
    public float mapWidth = 100f;
    public float mapDepth = 100f;

    public Vector3 mapCenter = Vector3.zero;

    public float baseRadius = 4f;

    public float angleJitterDegrees = 20f;

    public float meanderAmplitude = 8f;
    public float meanderFrequency = 1.5f;

    public int minIslands = 1;
    public int maxIslands = 2;
    public float islandRadiusMin = 3f;
    public float islandRadiusMax = 6f;
    public float islandOffsetRange = 10f;
    public float islandPushMargin = 3f;

    public int samplesPerPath = 60;

    public float minPathSeparation = 6f;

    public int maxGenerationAttempts = 30;

    public int seed = 0;
    public bool useRandomSeedOnAwake = true;

    public int pathCount = 3;

    public TerrainGenerator terrainGenerator;

    public List<GeneratedPath> Paths { get; private set; } = new List<GeneratedPath>();

    private System.Random rng;

    [System.Serializable]
    public class GeneratedPath
    {
        public List<Vector3> ControlPoints;
        public List<Vector3> SampledPoints;
        public float BaseAngle;
    }

    private void Awake()
    {
        if (useRandomSeedOnAwake)
            seed = System.Environment.TickCount;

        GenerateAllPaths();
    }
    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            if (terrainGenerator == null)
                terrainGenerator = GetComponent<TerrainGenerator>();
            terrainGenerator.GenerateTerrain();
        };
#endif
    }
    public void GenerateAllPaths()
    {
        rng = new System.Random(seed);
        Paths.Clear();

        float angleStep = 360f / pathCount;

        for (int i = 0; i < pathCount; i++)
        {
            float baseAngle = angleStep * i;
            GeneratedPath path = null;

            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                float jitteredAngle = baseAngle + RandomRange(-angleJitterDegrees, angleJitterDegrees);
                GeneratedPath candidate = GenerateSinglePath(jitteredAngle);

                if (IsPathAcceptable(candidate, Paths))
                {
                    path = candidate;
                    break;
                }

                if (attempt == maxGenerationAttempts - 1)
                    path = candidate;
            }

            Paths.Add(path);
        }
    }

    private GeneratedPath GenerateSinglePath(float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
        Vector3 edgePoint = GetRectangleEdgePoint(direction);
        Vector3 baseEdgePoint = mapCenter + direction * baseRadius;

        Vector3 spineDir = (baseEdgePoint - edgePoint).normalized;
        Vector3 perpendicular = Vector3.Cross(spineDir, Vector3.up).normalized;

        float primaryFreq = meanderFrequency * (float)(0.75 + rng.NextDouble() * 0.5);
        float primaryAmp = meanderAmplitude * (float)(0.7 + rng.NextDouble() * 0.6);
        float primaryPhase = RandomRange(0f, Mathf.PI * 2f);

        float secondaryFreq = primaryFreq * RandomRange(2.5f, 4f);
        float secondaryAmp = primaryAmp * RandomRange(0.2f, 0.4f);
        float secondaryPhase = RandomRange(0f, Mathf.PI * 2f);

        int islandCount = rng.Next(minIslands, maxIslands + 1);
        List<Vector3> islandCenters = new List<Vector3>();
        List<float> islandRadii = new List<float>();

        for (int i = 0; i < islandCount; i++)
        {
            float t = RandomRange(0.25f, 0.75f);
            Vector3 spinePoint = Vector3.Lerp(edgePoint, baseEdgePoint, t);
            float side = RandomRange(-islandOffsetRange, islandOffsetRange);

            islandCenters.Add(spinePoint + perpendicular * side);
            islandRadii.Add(RandomRange(islandRadiusMin, islandRadiusMax));
        }

        List<Vector3> sampledPoints = new List<Vector3>();
        List<Vector3> controlPoints = new List<Vector3>();

        for (int i = 0; i <= samplesPerPath; i++)
        {
            float t = i / (float)samplesPerPath;
            Vector3 spinePoint = Vector3.Lerp(edgePoint, baseEdgePoint, t);

            float envelope = Mathf.Sin(t * Mathf.PI);
            float offset = (primaryAmp * Mathf.Sin(t * primaryFreq * Mathf.PI * 2f + primaryPhase)
                          + secondaryAmp * Mathf.Sin(t * secondaryFreq * Mathf.PI * 2f + secondaryPhase)) * envelope;

            Vector3 point = spinePoint + perpendicular * offset;

            for (int isl = 0; isl < islandCenters.Count; isl++)
            {
                Vector3 toPoint = point - islandCenters[isl];
                float dist = toPoint.magnitude;
                float influenceRadius = islandRadii[isl] + islandPushMargin;

                if (dist < influenceRadius)
                {
                    float pushStrength = influenceRadius - dist;
                    Vector3 pushDir = dist > 0.001f ? toPoint.normalized : perpendicular;
                    point += pushDir * pushStrength;
                }
            }

            sampledPoints.Add(point);

            if (i % 5 == 0)
                controlPoints.Add(point);
        }

        sampledPoints[0] = edgePoint;
        sampledPoints[sampledPoints.Count - 1] = baseEdgePoint;

        return new GeneratedPath
        {
            ControlPoints = controlPoints,
            BaseAngle = angleDegrees,
            SampledPoints = sampledPoints
        };
    }

    private Vector3 GetRectangleEdgePoint(Vector3 direction)
    {
        float halfWidth = mapWidth * 0.5f;
        float halfDepth = mapDepth * 0.5f;

        float tx = Mathf.Approximately(direction.x, 0f) ? float.MaxValue : halfWidth / Mathf.Abs(direction.x);
        float tz = Mathf.Approximately(direction.z, 0f) ? float.MaxValue : halfDepth / Mathf.Abs(direction.z);

        float t = Mathf.Min(tx, tz);

        return mapCenter + direction * t;
    }

    private bool IsPathAcceptable(GeneratedPath candidate, List<GeneratedPath> existingPaths)
    {
        foreach (var existing in existingPaths)
        {
            foreach (var pointA in candidate.SampledPoints)
            {
                if (Vector3.Distance(pointA, mapCenter) < baseRadius * 1.5f)
                    continue;

                foreach (var pointB in existing.SampledPoints)
                {
                    if (Vector3.Distance(pointB, mapCenter) < baseRadius * 1.5f)
                        continue;

                    if (Vector3.Distance(pointA, pointB) < minPathSeparation)
                        return false;
                }
            }
        }
        return true;
    }

    public float GetNearestDistanceToAnyPath(Vector3 worldPos)
    {
        float minDist = float.MaxValue;
        foreach (var path in Paths)
        {
            foreach (var point in path.SampledPoints)
            {
                float dist = Vector3.Distance(
                    new Vector3(worldPos.x, 0f, worldPos.z),
                    new Vector3(point.x, 0f, point.z));
                if (dist < minDist) minDist = dist;
            }
        }
        return minDist;
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)(rng.NextDouble() * (max - min));
    }

    private void OnDrawGizmos()
    {
        if (Paths == null) return;

        Gizmos.color = Color.yellow;
        foreach (var path in Paths)
        {
            if (path?.SampledPoints == null) continue;
            for (int i = 0; i < path.SampledPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(path.SampledPoints[i], path.SampledPoints[i + 1]);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(mapCenter, baseRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapCenter, new Vector3(mapWidth, 0f, mapDepth));
    }
}
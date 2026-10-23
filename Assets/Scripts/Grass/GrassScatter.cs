using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scatters GPU-instanced grass tufts over the grassy part of the terrain,
/// avoiding the paths/river and the defender placement spots.
/// Colour and wind are handled entirely by the TerrainGrassURP shader.
/// </summary>
public class GrassScatter : MonoBehaviour
{
    [Header("References")]
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;
    public DefenderPlacementManager placementManager;

    [Header("Rendering")]
    [Tooltip("Optional custom tuft mesh. Leave empty to use a built-in low poly tuft.")]
    public Mesh grassMesh;
    [Tooltip("Material using Custom/TerrainGrassURP")]
    public Material grassMaterial;
    [Tooltip("Material whose colours the grass copies. Defaults to the terrain's material.")]
    public Material terrainMaterialOverride;

    [Header("Generation")]
    public bool generateOnStart = true;
    [Tooltip("Frames to wait so terrain + defender spots exist first.")]
    public int startDelayFrames = 2;
    public int seed = 12345;

    [Header("Scatter")]
    [Tooltip("Distance between grid points. Smaller = denser grass.")]
    public float spacing = 0.7f;
    [Range(0f, 1f)] public float jitter = 0.9f;
    public Vector2 heightRange = new Vector2(0.35f, 0.65f);
    public Vector2 widthRange = new Vector2(0.8f, 1.3f);
    [Tooltip("Sink the tuft slightly so the root never floats above the mesh.")]
    public float sink = 0.05f;
    public int maxInstances = 250000;

    [Header("Clumping")]
    public float clumpScale = 0.12f;
    [Range(0f, 1f)] public float clumpThreshold = 0.3f;

    [Header("Avoidance")]
    [Tooltip("Extra gap between the river bank edge and the first grass.")]
    public float pathMargin = 0.4f;
    [Tooltip("Extra gap around each defender spot (added to its spotRadius).")]
    public float towerClearance = 0.8f;
    [Tooltip("Keep grass this far inside the map border.")]
    public float edgeMargin = 1f;

    private class Batch
    {
        public Matrix4x4[] matrices;
        public int count;
        public Bounds bounds;
    }

    private readonly List<Batch> batches = new List<Batch>();
    private RenderParams renderParams;
    private bool ready;

    private static readonly string[] ColorProps =
        { "_GrassA", "_GrassB", "_GrassC", "_DirtA", "_DirtB", "_WetColor", "_ShadowTint" };
    private static readonly string[] FloatProps =
        { "_PatchScale", "_FaceVariation", "_PathThreshold", "_PathSoftness", "_EdgeNoise",
          "_Saturation", "_Bands", "_AmbientStrength" };

    private IEnumerator Start()
    {
        if (!generateOnStart) yield break;
        for (int i = 0; i < startDelayFrames; i++) yield return null;
        Generate();
    }

    public void Generate()
    {
        batches.Clear();
        ready = false;

        if (pathGenerator == null || terrainGenerator == null || grassMaterial == null)
        {
            Debug.LogWarning("GrassScatter: assign pathGenerator, terrainGenerator and grassMaterial.");
            return;
        }

        if (grassMesh == null)
            grassMesh = BuildDefaultTuft();

        SyncMaterialWithTerrain();
        grassMaterial.enableInstancing = true;

        float halfW = pathGenerator.mapWidth * 0.5f - edgeMargin;
        float halfD = pathGenerator.mapDepth * 0.5f - edgeMargin;
        Vector3 center = pathGenerator.mapCenter;

        // grass starts where the terrain's path influence is fully zero (+ margin)
        float minPathDist = terrainGenerator.riverBedWidth + terrainGenerator.riverBankWidth + pathMargin;

        // collect defender spot positions
        List<Vector3> spotPositions = new List<Vector3>();
        float towerClear = towerClearance;
        if (placementManager != null)
        {
            towerClear += placementManager.spotRadius;
            foreach (var s in placementManager.Spots)
                if (s != null) spotPositions.Add(s.transform.position);
        }
        float towerClearSqr = towerClear * towerClear;

        System.Random rng = new System.Random(seed);
        float noiseOffX = (float)rng.NextDouble() * 1000f;
        float noiseOffZ = (float)rng.NextDouble() * 1000f;

        // process the map in square tiles so each draw batch is spatially compact (good culling)
        const float tileSize = 20f;
        List<Matrix4x4> tileMats = new List<Matrix4x4>();
        int total = 0;

        float startX = center.x - halfW;
        float startZ = center.z - halfD;
        float endX = center.x + halfW;
        float endZ = center.z + halfD;

        for (float tz = startZ; tz < endZ; tz += tileSize)
        {
            for (float tx = startX; tx < endX; tx += tileSize)
            {
                tileMats.Clear();
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                float tileEndX = Mathf.Min(tx + tileSize, endX);
                float tileEndZ = Mathf.Min(tz + tileSize, endZ);

                for (float z = tz; z < tileEndZ; z += spacing)
                {
                    for (float x = tx; x < tileEndX; x += spacing)
                    {
                        if (total >= maxInstances) break;

                        float px = x + ((float)rng.NextDouble() - 0.5f) * spacing * jitter * 2f;
                        float pz = z + ((float)rng.NextDouble() - 0.5f) * spacing * jitter * 2f;

                        // random draws happen before the rejection tests so layout stays stable
                        float yaw = (float)rng.NextDouble() * 360f;
                        float h = Mathf.Lerp(heightRange.x, heightRange.y, (float)rng.NextDouble());
                        float w = Mathf.Lerp(widthRange.x, widthRange.y, (float)rng.NextDouble());

                        if (px < startX || px > endX || pz < startZ || pz > endZ) continue;

                        // clumping noise
                        float n = Mathf.PerlinNoise((px + noiseOffX) * clumpScale, (pz + noiseOffZ) * clumpScale);
                        if (n < clumpThreshold) continue;

                        // avoid paths / river
                        if (pathGenerator.GetNearestDistanceToAnyPath(new Vector3(px, 0f, pz)) < minPathDist)
                            continue;

                        // avoid defender spots
                        bool nearTower = false;
                        for (int i = 0; i < spotPositions.Count; i++)
                        {
                            float dx = spotPositions[i].x - px;
                            float dz = spotPositions[i].z - pz;
                            if (dx * dx + dz * dz < towerClearSqr) { nearTower = true; break; }
                        }
                        if (nearTower) continue;

                        float py = terrainGenerator.SampleHeight(px, pz) - sink;
                        Vector3 pos = new Vector3(px, py, pz);

                        tileMats.Add(Matrix4x4.TRS(pos, Quaternion.Euler(0f, yaw, 0f), new Vector3(w, h, w)));
                        min = Vector3.Min(min, pos);
                        max = Vector3.Max(max, pos);
                        total++;
                    }
                }

                if (tileMats.Count == 0) continue;

                Bounds b = new Bounds((min + max) * 0.5f, max - min);
                b.Expand(new Vector3(2f, 3f, 2f)); // tuft size + wind movement

                // split the tile into chunks of 1023 (instancing limit)
                for (int start = 0; start < tileMats.Count; start += 1023)
                {
                    int count = Mathf.Min(1023, tileMats.Count - start);
                    Batch batch = new Batch
                    {
                        matrices = new Matrix4x4[count],
                        count = count,
                        bounds = b
                    };
                    tileMats.CopyTo(start, batch.matrices, 0, count);
                    batches.Add(batch);
                }
            }
        }

        renderParams = new RenderParams(grassMaterial)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = true,
            layer = gameObject.layer
        };

        ready = true;
        Debug.Log($"GrassScatter: placed {total} grass tufts in {batches.Count} batches.");
    }

    private void Update()
    {
        if (!ready) return;

        for (int i = 0; i < batches.Count; i++)
        {
            renderParams.worldBounds = batches[i].bounds;
            Graphics.RenderMeshInstanced(renderParams, grassMesh, 0, batches[i].matrices, batches[i].count);
        }
    }

    /// <summary>Copies the terrain colour settings so the grass always matches.</summary>
    public void SyncMaterialWithTerrain()
    {
        Material src = terrainMaterialOverride != null ? terrainMaterialOverride : terrainGenerator.terrainMaterial;
        if (src == null) return;

        foreach (string p in ColorProps)
            if (src.HasProperty(p) && grassMaterial.HasProperty(p))
                grassMaterial.SetColor(p, src.GetColor(p));

        foreach (string p in FloatProps)
            if (src.HasProperty(p) && grassMaterial.HasProperty(p))
                grassMaterial.SetFloat(p, src.GetFloat(p));

        // cell size must match the terrain mesh grid
        if (grassMaterial.HasProperty("_CellSize") && pathGenerator != null)
        {
            float cell = pathGenerator.mapWidth / Mathf.Max(1, terrainGenerator.resolution);
            grassMaterial.SetFloat("_CellSize", cell);
            if (src.HasProperty("_CellSize")) src.SetFloat("_CellSize", cell);
        }
    }

    /// <summary>
    /// Low poly tuft: a handful of tapered blades. Pivot at the base,
    /// uv.y = 0 at the root and 1 at the tip (used by the shader for wind + shading).
    /// </summary>
    public static Mesh BuildDefaultTuft(int blades = 5)
    {
        System.Random r = new System.Random(1234);
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i < blades; i++)
        {
            float ang = i / (float)blades * Mathf.PI * 2f + (float)r.NextDouble() * 0.8f;
            float radius = 0.05f + (float)r.NextDouble() * 0.12f;
            float height = 0.75f + (float)r.NextDouble() * 0.45f;
            float width = 0.06f + (float)r.NextDouble() * 0.03f;
            float facing = (float)r.NextDouble() * Mathf.PI;
            float lean = 0.1f + (float)r.NextDouble() * 0.2f;

            Vector3 basePos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Vector3 right = new Vector3(Mathf.Cos(facing), 0f, Mathf.Sin(facing));
            Vector3 leanDir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));

            Vector3 mid = basePos + leanDir * lean * 0.4f + Vector3.up * height * 0.5f;
            Vector3 tip = basePos + leanDir * lean + Vector3.up * height;

            int s = verts.Count;
            verts.Add(basePos - right * width); uvs.Add(new Vector2(0f, 0f));
            verts.Add(basePos + right * width); uvs.Add(new Vector2(1f, 0f));
            verts.Add(mid - right * width * 0.7f); uvs.Add(new Vector2(0f, 0.5f));
            verts.Add(mid + right * width * 0.7f); uvs.Add(new Vector2(1f, 0.5f));
            verts.Add(tip); uvs.Add(new Vector2(0.5f, 1f));
            for (int k = 0; k < 5; k++) normals.Add(Vector3.up);

            tris.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3, s + 2, s + 4, s + 3 });
        }

        Mesh m = new Mesh { name = "GrassTuft" };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetNormals(normals);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }
}
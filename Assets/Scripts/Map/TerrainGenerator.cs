using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{
    public PathGenerator pathGenerator;

    public int resolution = 150;
    public float plateauHeight = 6f;
    public float riverBedWidth = 4f;
    public float riverBankWidth = 4f;
    public float plateauNoiseStrength = 0.3f;
    public float riverNoiseStrength = 0.2f;
    public float noiseScale = 0.05f;
    public bool generateCollider = true;
    public Material terrainMaterial;

    [Header("Outer Terrain")]
    [Tooltip("How far the terrain continues past the map edge. Keep this above Path Extension + 15.")]
    public float edgePadding = 40f;
    [Tooltip("Height the hills reach at the outer edge.")]
    public float edgeRise = 12f;
    public float edgeRiseNoise = 4f;
    public float edgeRiseNoiseScale = 0.08f;
    [Tooltip("How far from a path the hills take to reach full height.")]
    public float valleyWidth = 18f;
    [Tooltip("Above 1 keeps the valley floor wider before the slopes start.")]
    public float valleyShape = 1.5f;
    [Range(0f, 1f)]
    [Tooltip("How much of the hill rise the path floor keeps. 0 = cut to the ground, 1 = no valley.")]
    public float valleyFloorRise = 0.35f;

    public bool useDebugVertexColors = false;
    public Color debugRiverColor = new Color(0.5f, 0.35f, 0.2f);
    public Color debugPlateauColor = new Color(0.3f, 0.6f, 0.3f);

    private Mesh mesh;
    private float noiseOffsetX;
    private float noiseOffsetZ;

    private void Awake()
    {
        if (pathGenerator == null)
            pathGenerator = GetComponent<PathGenerator>();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            GenerateTerrain();
        };
#endif
    }

    private void Start()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (pathGenerator == null)
            pathGenerator = GetComponent<PathGenerator>();

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        pathGenerator.GenerateAllPaths();

        noiseOffsetX = Random.Range(0f, 9999f);
        noiseOffsetZ = Random.Range(0f, 9999f);

        BuildMesh();
    }

    private void BuildMesh()
    {
        int baseRes = Mathf.Max(1, resolution);
        float halfWidth = pathGenerator.mapWidth * 0.5f;
        float halfDepth = pathGenerator.mapDepth * 0.5f;

        float cellX = pathGenerator.mapWidth / baseRes;
        float cellZ = pathGenerator.mapDepth / baseRes;

        int padX = Mathf.CeilToInt(Mathf.Max(0f, edgePadding) / cellX);
        int padZ = Mathf.CeilToInt(Mathf.Max(0f, edgePadding) / cellZ);

        int resX = baseRes + padX * 2;
        int resZ = baseRes + padZ * 2;
        int vertsX = resX + 1;
        int vertsZ = resZ + 1;

        float startX = pathGenerator.mapCenter.x - halfWidth - padX * cellX;
        float startZ = pathGenerator.mapCenter.z - halfDepth - padZ * cellZ;

        Vector3[] vertices = new Vector3[vertsX * vertsZ];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[resX * resZ * 6];

        for (int z = 0; z <= resZ; z++)
        {
            for (int x = 0; x <= resX; x++)
            {
                int index = z * vertsX + x;

                float worldX = startX + x * cellX;
                float worldZ = startZ + z * cellZ;

                float pathDist = PathDistance(worldX, worldZ);
                float pathInfluence = InfluenceFromDistance(pathDist);
                float height = CalculateHeight(worldX, worldZ, pathInfluence, pathDist);

                vertices[index] = new Vector3(worldX, height, worldZ);
                uvs[index] = new Vector2(x / (float)resX, z / (float)resZ);
                colors[index] = useDebugVertexColors
                    ? Color.Lerp(debugPlateauColor, debugRiverColor, pathInfluence)
                    : new Color(pathInfluence, 0f, 0f, 1f);
            }
        }

        int triIndex = 0;
        for (int z = 0; z < resZ; z++)
        {
            for (int x = 0; x < resX; x++)
            {
                int topLeft = z * vertsX + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * vertsX + x;
                int bottomRight = bottomLeft + 1;

                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;

                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (useDebugVertexColors)
        {
            renderer.sharedMaterial = GetDebugVertexColorMaterial();
        }
        else if (terrainMaterial != null)
        {
            renderer.sharedMaterial = terrainMaterial;
        }
        else if (renderer.sharedMaterial == null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        if (generateCollider)
        {
            MeshCollider collider = GetComponent<MeshCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }
    }

    private float PathDistance(float worldX, float worldZ)
    {
        return pathGenerator.GetNearestDistanceToAnyPath(new Vector3(worldX, 0f, worldZ));
    }

    private float InfluenceFromDistance(float dist)
    {
        float t = Mathf.Clamp01(Mathf.InverseLerp(riverBedWidth, riverBedWidth + riverBankWidth, dist));
        float smooth = t * t * (3f - 2f * t);
        return 1f - smooth;
    }

    private float ValleyFactor(float dist)
    {
        float t = Mathf.Clamp01(Mathf.InverseLerp(riverBedWidth, riverBedWidth + Mathf.Max(valleyWidth, 0.01f), dist));
        t = Mathf.Pow(t, valleyShape);
        return t * t * (3f - 2f * t);
    }

    private float OutsideDistance(float worldX, float worldZ)
    {
        float dx = Mathf.Abs(worldX - pathGenerator.mapCenter.x) - pathGenerator.mapWidth * 0.5f;
        float dz = Mathf.Abs(worldZ - pathGenerator.mapCenter.z) - pathGenerator.mapDepth * 0.5f;
        dx = Mathf.Max(dx, 0f);
        dz = Mathf.Max(dz, 0f);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private float CalculateHeight(float worldX, float worldZ, float pathInfluence, float pathDist)
    {
        float plateauNoise = Mathf.PerlinNoise((worldX + noiseOffsetX) * noiseScale, (worldZ + noiseOffsetZ) * noiseScale);
        float riverNoise = Mathf.PerlinNoise((worldX + noiseOffsetX) * noiseScale * 2f, (worldZ + noiseOffsetZ) * noiseScale * 2f);

        float baseHeight = plateauHeight * (1f - pathInfluence);
        float noiseContribution = Mathf.Lerp(plateauNoise * plateauNoiseStrength, riverNoise * riverNoiseStrength, pathInfluence);

        float height = baseHeight + noiseContribution;

        float outside = OutsideDistance(worldX, worldZ);
        if (outside > 0f)
        {
            float t = Mathf.Clamp01(outside / Mathf.Max(edgePadding, 0.01f));
            t = t * t * (3f - 2f * t);

            float hillNoise = Mathf.PerlinNoise((worldX + noiseOffsetX) * edgeRiseNoiseScale, (worldZ + noiseOffsetZ) * edgeRiseNoiseScale);
            float valley = Mathf.Lerp(valleyFloorRise, 1f, ValleyFactor(pathDist));
            height += (edgeRise + hillNoise * edgeRiseNoise) * t * valley;
        }

        return height;
    }

    public float SampleHeight(float worldX, float worldZ)
    {
        float dist = PathDistance(worldX, worldZ);
        return CalculateHeight(worldX, worldZ, InfluenceFromDistance(dist), dist);
    }

    private Material debugMaterial;

    private Material GetDebugVertexColorMaterial()
    {
        if (debugMaterial != null)
            return debugMaterial;

        Shader shader = Shader.Find("Custom/VertexColorUnlit");
        if (shader == null)
        {
            Debug.LogWarning("Custom/VertexColorUnlit shader not found - add VertexColorUnlit.shader to your project.");
            shader = Shader.Find("Standard");
        }

        debugMaterial = new Material(shader);
        return debugMaterial;
    }
}
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
        int vertsPerSide = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerSide * vertsPerSide];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        float halfWidth = pathGenerator.mapWidth * 0.5f;
        float halfDepth = pathGenerator.mapDepth * 0.5f;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = z * vertsPerSide + x;

                float worldX = Mathf.Lerp(-halfWidth, halfWidth, x / (float)resolution) + pathGenerator.mapCenter.x;
                float worldZ = Mathf.Lerp(-halfDepth, halfDepth, z / (float)resolution) + pathGenerator.mapCenter.z;

                float pathInfluence = GetPathInfluence(worldX, worldZ);
                float height = CalculateHeight(worldX, worldZ, pathInfluence);

                vertices[index] = new Vector3(worldX, height, worldZ);
                uvs[index] = new Vector2(x / (float)resolution, z / (float)resolution);
                colors[index] = Color.Lerp(debugPlateauColor, debugRiverColor, pathInfluence);
            }
        }

        int triIndex = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int topLeft = z * vertsPerSide + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * vertsPerSide + x;
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
        if (useDebugVertexColors)
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

    private float GetPathInfluence(float worldX, float worldZ)
    {
        Vector3 worldPos = new Vector3(worldX, 0f, worldZ);
        float dist = pathGenerator.GetNearestDistanceToAnyPath(worldPos);

        float t = Mathf.InverseLerp(riverBedWidth, riverBedWidth + riverBankWidth, dist);
        t = Mathf.Clamp01(t);
        float smooth = t * t * (3f - 2f * t);

        return 1f - smooth;
    }

    private float CalculateHeight(float worldX, float worldZ, float pathInfluence)
    {
        float plateauNoise = Mathf.PerlinNoise((worldX + noiseOffsetX) * noiseScale, (worldZ + noiseOffsetZ) * noiseScale);
        float riverNoise = Mathf.PerlinNoise((worldX + noiseOffsetX) * noiseScale * 2f, (worldZ + noiseOffsetZ) * noiseScale * 2f);

        float baseHeight = plateauHeight * (1f - pathInfluence);
        float noiseContribution = Mathf.Lerp(plateauNoise * plateauNoiseStrength, riverNoise * riverNoiseStrength, pathInfluence);

        return baseHeight + noiseContribution;
    }

    public float SampleHeight(float worldX, float worldZ)
    {
        return CalculateHeight(worldX, worldZ, GetPathInfluence(worldX, worldZ));
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
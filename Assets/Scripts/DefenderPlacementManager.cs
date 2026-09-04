using System.Collections.Generic;
using UnityEngine;

public class DefenderPlacementManager : MonoBehaviour
{
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;
    public DefenderFactory defenderFactory;

    public int spotCount = 10;
    public float minSpotSeparation = 6f;
    public float minDistanceFromPath = 5f;
    public float spotRadius = 1.5f;
    public GameObject spotMarkerPrefab;

    private List<DefenderPlacementSpot> spots = new List<DefenderPlacementSpot>();

    public void GenerateSpots()
    {
        foreach (var s in spots)
        {
            if (s != null)
                Destroy(s.gameObject);
        }
        spots.Clear();

        float halfWidth = pathGenerator.mapWidth * 0.5f;
        float halfDepth = pathGenerator.mapDepth * 0.5f;

        System.Random rng = new System.Random();
        int attempts = 0;
        int maxAttempts = spotCount * 50;

        while (spots.Count < spotCount && attempts < maxAttempts)
        {
            attempts++;

            float x = pathGenerator.mapCenter.x + (float)(rng.NextDouble() * 2f - 1f) * halfWidth * 0.85f;
            float z = pathGenerator.mapCenter.z + (float)(rng.NextDouble() * 2f - 1f) * halfDepth * 0.85f;

            Vector3 candidateXZ = new Vector3(x, 0f, z);

            if (pathGenerator.GetNearestDistanceToAnyPath(candidateXZ) < minDistanceFromPath)
                continue;

            if (Vector3.Distance(candidateXZ, pathGenerator.mapCenter) < pathGenerator.baseRadius * 2f)
                continue;

            bool tooClose = false;
            foreach (var s in spots)
            {
                if (Vector3.Distance(s.transform.position, candidateXZ) < minSpotSeparation)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            float y = terrainGenerator.SampleHeight(x, z);
            Vector3 spotPos = new Vector3(x, y, z);

            GameObject spotObj = spotMarkerPrefab != null
                ? Instantiate(spotMarkerPrefab, spotPos, Quaternion.identity, transform)
                : new GameObject("PlacementSpot");

            spotObj.transform.position = spotPos;
            spotObj.transform.SetParent(transform);

            if (spotObj.GetComponent<Collider>() == null)
            {
                SphereCollider col = spotObj.AddComponent<SphereCollider>();
                col.radius = spotRadius;
            }

            DefenderPlacementSpot spot = spotObj.GetComponent<DefenderPlacementSpot>();
            if (spot == null)
                spot = spotObj.AddComponent<DefenderPlacementSpot>();

            spot.Initialize(defenderFactory);
            spots.Add(spot);
        }
    }
}
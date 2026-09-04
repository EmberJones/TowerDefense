using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]
public class MainTower : MonoBehaviour
{
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    public void PlaceAtCenter()
    {
        float y = terrainGenerator.SampleHeight(pathGenerator.mapCenter.x, pathGenerator.mapCenter.z);
        transform.position = new Vector3(pathGenerator.mapCenter.x, y, pathGenerator.mapCenter.z);
    }

    private void HandleDeath()
    {
        GameManager.Instance?.GameOver();
    }
}
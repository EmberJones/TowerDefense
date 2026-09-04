using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;
    public EnemyFactory enemyFactory;

    public float spawnInterval = 3f;

    private float timer;
    private int nextPathIndex;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (pathGenerator.Paths == null || pathGenerator.Paths.Count == 0)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        var path = pathGenerator.Paths[nextPathIndex];
        nextPathIndex = (nextPathIndex + 1) % pathGenerator.Paths.Count;

        Vector3 spawnPos = path.SampledPoints[0];
        spawnPos.y = terrainGenerator.SampleHeight(spawnPos.x, spawnPos.z);

        enemyFactory.CreateEnemy(EnemyFactory.EnemyType.Basic, spawnPos, path.SampledPoints, terrainGenerator);
    }
}
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;
    public EnemyFactory enemyFactory;

    public float baseSpawnInterval = 2f;
    public int baseEnemiesPerWave = 5;
    public float enemiesPerWaveGrowth = 1.5f;
    public float timeBetweenWaves = 8f;

    private int waveNumber;
    private int nextPathIndex;
    private int enemiesLeakedThisWave;
    private int enemiesKilledThisWave;
    private float difficultyMultiplier = 1f;

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        while (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            waveNumber++;
            yield return StartCoroutine(SpawnWave());
            AdjustDifficulty();
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave()
    {
        enemiesLeakedThisWave = 0;
        enemiesKilledThisWave = 0;

        int enemyCount = Mathf.RoundToInt((baseEnemiesPerWave + waveNumber * enemiesPerWaveGrowth) * difficultyMultiplier);
        float interval = baseSpawnInterval / Mathf.Max(difficultyMultiplier, 0.5f);

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(PickEnemyType());
            yield return new WaitForSeconds(interval);
        }
    }

    private EnemyFactory.EnemyType PickEnemyType()
    {
        float rangedChance = Mathf.Clamp01((waveNumber - 2) * 0.08f);
        float rusherChance = Mathf.Clamp01((waveNumber - 4) * 0.06f);

        float roll = Random.value;
        if (roll < rusherChance) return EnemyFactory.EnemyType.Ghost;
        if (roll < rusherChance + rangedChance) return EnemyFactory.EnemyType.Vampire;
        return EnemyFactory.EnemyType.Zombie;
    }

    private void SpawnEnemy(EnemyFactory.EnemyType type)
    {
        if (pathGenerator.Paths == null || pathGenerator.Paths.Count == 0) return;

        var path = pathGenerator.Paths[nextPathIndex];
        nextPathIndex = (nextPathIndex + 1) % pathGenerator.Paths.Count;

        Vector3 spawnPos = path.SampledPoints[0];
        spawnPos.y = terrainGenerator.SampleHeight(spawnPos.x, spawnPos.z);

        Enemy enemy = enemyFactory.CreateEnemy(type, spawnPos, path.SampledPoints, terrainGenerator);
        if (enemy == null) return;

        enemy.OnReachedMainTower += () => enemiesLeakedThisWave++;

        Health health = enemy.GetComponent<Health>();
        if (health != null)
            health.OnDeath += () => enemiesKilledThisWave++;
    }

    private void AdjustDifficulty()
    {
        int totalTracked = enemiesKilledThisWave + enemiesLeakedThisWave;
        float leakRatio = totalTracked > 0 ? (float)enemiesLeakedThisWave / totalTracked : 0f;

        float towerHealthPercent = 1f;
        if (GameManager.Instance != null && GameManager.Instance.mainTower != null)
        {
            Health th = GameManager.Instance.mainTower.GetComponent<Health>();
            if (th != null && th.MaxHealth > 0)
                towerHealthPercent = (float)th.CurrentHealth / th.MaxHealth;
        }

        if (leakRatio > 0.3f || towerHealthPercent < 0.4f)
        {
            difficultyMultiplier = Mathf.Max(0.6f, difficultyMultiplier - 0.15f);
        }
        else if (leakRatio < 0.1f && towerHealthPercent > 0.8f)
        {
            difficultyMultiplier = Mathf.Min(2.5f, difficultyMultiplier + 0.15f);
        }
    }
}
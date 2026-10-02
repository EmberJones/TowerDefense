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

    [SerializeField] private float rangedChanceMult = 0.6f;
    [SerializeField] private float rusherChanceMult = 0.4f;

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

        Debug.Log($"<color=cyan>[WaveManager]</color> Wave {waveNumber} config: " +
                  $"enemyCount={enemyCount}, spawnInterval={interval:F2}s, " +
                  $"baseEnemies={baseEnemiesPerWave}, growth={enemiesPerWaveGrowth}, diffMult={difficultyMultiplier:F2}");

        for (int i = 0; i < enemyCount; i++)
        {
            EnemyFactory.EnemyType type = PickEnemyType();
            SpawnEnemy(type);
            yield return new WaitForSeconds(interval);
        }
    }

    private EnemyFactory.EnemyType PickEnemyType()
    {
        float rangedChance = Mathf.Clamp01((waveNumber - 5) * rangedChanceMult);
        float rusherChance = Mathf.Clamp01((waveNumber - 2) * rusherChanceMult);

        float roll = Random.value;
        EnemyFactory.EnemyType chosen;
        if (roll < rusherChance) chosen = EnemyFactory.EnemyType.Ghost;
        else if (roll < rusherChance + rangedChance) chosen = EnemyFactory.EnemyType.Vampire;
        else chosen = EnemyFactory.EnemyType.Zombie;

        Debug.Log($"<color=grey>[WaveManager]</color> PickEnemyType: wave={waveNumber}, " +
                  $"roll={roll:F2}, rusherChance={rusherChance:F2}, rangedChance={rangedChance:F2} " +
                  $"=> {chosen}");

        return chosen;
    }

    private void SpawnEnemy(EnemyFactory.EnemyType type)
    {
        if (pathGenerator.Paths == null || pathGenerator.Paths.Count == 0)
        {
            Debug.LogWarning("<color=red>[WaveManager]</color> SpawnEnemy aborted: no paths available!");
            return;
        }

        var path = pathGenerator.Paths[nextPathIndex];
        nextPathIndex = (nextPathIndex + 1) % pathGenerator.Paths.Count;

        Vector3 spawnPos = path.SampledPoints[0];
        spawnPos.y = terrainGenerator.SampleHeight(spawnPos.x, spawnPos.z);

        Enemy enemy = enemyFactory.CreateEnemy(type, spawnPos, path.SampledPoints, terrainGenerator);
        if (enemy == null)
        {
            Debug.LogError("<color=red>[WaveManager]</color> EnemyFactory returned null! Enemy not spawned.");
            return;
        }

        enemy.OnReachedMainTower += () =>
        {
            enemiesLeakedThisWave++;
        };

        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath += () =>
            {
                enemiesKilledThisWave++;
            };
        }
        else
        {
            Debug.LogWarning($"<color=orange>[WaveManager]</color> Spawned {type} has no Health component — " +
                             "kill tracking will not work for it.");
        }
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
        else
        {
            Debug.LogWarning("<color=orange>[WaveManager]</color> AdjustDifficulty: GameManager or mainTower is null. " +
                             "Tower health defaulted to 100%.");
        }

        float previousMultiplier = difficultyMultiplier;

        if (leakRatio > 0.3f || towerHealthPercent < 0.4f)
        {
            difficultyMultiplier = Mathf.Max(0.6f, difficultyMultiplier - 0.15f);
            Debug.Log($"<color=orange>[WaveManager]</color> Difficulty DECREASED because " +
                      $"(leakRatio={leakRatio:F2} > 0.3 || towerHP={towerHealthPercent:F2} < 0.4). " +
                      $"{previousMultiplier:F2} -> {difficultyMultiplier:F2}");
        }
        else if (leakRatio < 0.1f && towerHealthPercent > 0.8f)
        {
            difficultyMultiplier = Mathf.Min(2.5f, difficultyMultiplier + 0.15f);
            Debug.Log($"<color=lime>[WaveManager]</color> Difficulty INCREASED because " +
                      $"(leakRatio={leakRatio:F2} < 0.1 && towerHP={towerHealthPercent:F2} > 0.8). " +
                      $"{previousMultiplier:F2} -> {difficultyMultiplier:F2}");
        }
        else
        {
            Debug.Log($"<color=grey>[WaveManager]</color> Difficulty UNCHANGED. " +
                      $"leakRatio={leakRatio:F2}, towerHP={towerHealthPercent:F2}, " +
                      $"multiplier stays at {difficultyMultiplier:F2}");
        }

        Debug.Log($"<color=cyan>[WaveManager]</color> AdjustDifficulty summary for wave {waveNumber}: " +
                  $"killed={enemiesKilledThisWave}, leaked={enemiesLeakedThisWave}, " +
                  $"totalTracked={totalTracked}, leakRatio={leakRatio:F2}, " +
                  $"towerHP%={towerHealthPercent:F2}");
    }
}
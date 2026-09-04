using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    public enum EnemyType { Basic }

    [System.Serializable]
    public struct EnemyEntry
    {
        public EnemyType type;
        public GameObject prefab;
    }

    public List<EnemyEntry> enemyPrefabs;

    public Enemy CreateEnemy(EnemyType type, Vector3 position, List<Vector3> path, TerrainGenerator terrain)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        Enemy enemy = instance.GetComponent<Enemy>();
        enemy.Initialize(path, terrain);
        return enemy;
    }

    private GameObject GetPrefab(EnemyType type)
    {
        foreach (var entry in enemyPrefabs)
        {
            if (entry.type == type)
                return entry.prefab;
        }
        return null;
    }
}
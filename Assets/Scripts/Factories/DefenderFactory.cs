using System.Collections.Generic;
using UnityEngine;

public class DefenderFactory : MonoBehaviour
{
    public enum DefenderType { Wizard, Knight, Archer }

    [System.Serializable]
    public struct TowerEntry
    {
        public DefenderType type;
        public GameObject prefab;
    }

    public List<TowerEntry> towerPrefabs;

    public Defender CreateTower(DefenderType type, Vector3 position)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        return instance.GetComponent<Defender>();
    }

    private GameObject GetPrefab(DefenderType type)
    {
        foreach (var entry in towerPrefabs)
        {
            if (entry.type == type)
                return entry.prefab;
        }
        return null;
    }
}
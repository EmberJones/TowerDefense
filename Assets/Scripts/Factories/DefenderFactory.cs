using System.Collections.Generic;
using UnityEngine;

public class DefenderFactory : MonoBehaviour
{
    public enum DefenderType { Basic }

    [System.Serializable]
    public struct DefenderEntry
    {
        public DefenderType type;
        public GameObject prefab;
    }

    public List<DefenderEntry> defenderPrefabs;
    public Defender CreateDefender(DefenderType type, Vector3 position)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        return instance.GetComponent<Defender>();
    }

    private GameObject GetPrefab(DefenderType type)
    {
        foreach (var entry in defenderPrefabs)
        {
            if (entry.type == type)
                return entry.prefab;
        }
        return null;
    }
}
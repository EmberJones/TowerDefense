using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]
public class MainTower : MonoBehaviour
{
    public PathGenerator pathGenerator;
    public TerrainGenerator terrainGenerator;

    private Health health;
    private MainTowerUpgrades upgrades;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;

        upgrades = GetComponent<MainTowerUpgrades>();
        if (upgrades == null)
            upgrades = gameObject.AddComponent<MainTowerUpgrades>();
    }

    private void OnMouseUpAsButton()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        MainTowerUpgradeUI.Instance?.Open(upgrades);
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
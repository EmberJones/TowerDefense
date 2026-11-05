using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public TerrainGenerator terrainGenerator;
    public DefenderPlacementManager placementManager;
    public MainTower mainTower;

    public bool IsGameOver { get; private set; }

    public event Action OnGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        terrainGenerator.GenerateTerrain();
        placementManager.GenerateSpots();
        mainTower.PlaceAtCenter();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShopUI.Instance?.Close();
            UpgradeUI.Instance?.Close();
            MainTowerUpgradeUI.Instance?.Close();
        }
        if (Input.GetMouseButtonDown(0) &&
    UnityEngine.EventSystems.EventSystem.current != null &&
    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            ShopUI.Instance?.Close();
            UpgradeUI.Instance?.Close();
            MainTowerUpgradeUI.Instance?.Close();
        }
    }

    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        OnGameOver?.Invoke();
        Debug.Log("Game Over");
        Time.timeScale = 0f;
    }
}
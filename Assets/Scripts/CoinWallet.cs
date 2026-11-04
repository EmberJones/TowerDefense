using System;
using UnityEngine;

public class CoinWallet : MonoBehaviour
{
    private static CoinWallet instance;

    public static CoinWallet Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<CoinWallet>();
            if (instance == null)
                instance = new GameObject("CoinWallet").AddComponent<CoinWallet>();
            return instance;
        }
    }

    [SerializeField] private int startingCoins = 30;
    [SerializeField] private int towerCost = 10;
    [SerializeField] private int basicUpgradeCost = 15;
    [SerializeField] private int comboUpgradeCost = 30;
    [SerializeField] private int mainTowerUpgradeCost = 10;

    public int Coins { get; private set; }
    public int TowerCost => towerCost;
    public int MainTowerUpgradeCost => mainTowerUpgradeCost;

    public event Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Coins = startingCoins;
        OnCoinsChanged?.Invoke(Coins);
    }

    public int UpgradeCost(UpgradeSlot slot)
    {
        bool basic = slot == UpgradeSlot.Left || slot == UpgradeSlot.Middle || slot == UpgradeSlot.Right;
        return basic ? basicUpgradeCost : comboUpgradeCost;
    }

    public bool CanAfford(int cost)
    {
        return Coins >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (cost > Coins) return false;

        Coins -= cost;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }
}
using System;
using UnityEngine;

public enum MainTowerStat
{
    MaxHealth,
    Regen,
    Armor
}

[RequireComponent(typeof(Health))]
public class MainTowerUpgrades : MonoBehaviour
{
    [SerializeField] private int maxLevel = 3;
    [SerializeField] private int maxHealthPerLevel = 50;
    [SerializeField] private float regenPerLevel = 2f;
    [Range(0.5f, 0.99f)]
    [SerializeField] private float armorMultiplierPerLevel = 0.9f;

    private readonly int[] levels = new int[3];
    private Health health;
    private float regenBuffer;

    public event Action OnUpgraded;

    public int MaxLevel => maxLevel;
    public int Cost => CoinWallet.Instance.MainTowerUpgradeCost;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Update()
    {
        float regenPerSecond = levels[(int)MainTowerStat.Regen] * regenPerLevel;
        if (regenPerSecond <= 0f) return;

        regenBuffer += regenPerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(regenBuffer);
        if (whole > 0)
        {
            health.Heal(whole);
            regenBuffer -= whole;
        }
    }

    public int GetLevel(MainTowerStat stat)
    {
        return levels[(int)stat];
    }

    public bool IsMaxed(MainTowerStat stat)
    {
        return levels[(int)stat] >= maxLevel;
    }

    public string Describe(MainTowerStat stat)
    {
        switch (stat)
        {
            case MainTowerStat.MaxHealth:
                return $"+{maxHealthPerLevel} Max HP";
            case MainTowerStat.Regen:
                return $"+{regenPerLevel:0.#} HP/sec";
            default:
                return $"-{Mathf.RoundToInt((1f - armorMultiplierPerLevel) * 100f)}% damage taken";
        }
    }

    public bool TryUpgrade(MainTowerStat stat)
    {
        if (IsMaxed(stat)) return false;
        if (!CoinWallet.Instance.TrySpend(Cost)) return false;

        levels[(int)stat]++;

        switch (stat)
        {
            case MainTowerStat.MaxHealth:
                health.IncreaseMaxHealth(maxHealthPerLevel);
                break;
            case MainTowerStat.Armor:
                health.damageReductionMultiplier *= armorMultiplierPerLevel;
                break;
        }

        OnUpgraded?.Invoke();
        return true;
    }
}
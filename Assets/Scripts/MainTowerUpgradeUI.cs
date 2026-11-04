using UnityEngine;
using UnityEngine.UI;

public class MainTowerUpgradeUI : MonoBehaviour
{
    public static MainTowerUpgradeUI Instance { get; private set; }

    public GameObject panelRoot;
    public Button healthButton;
    public Button regenButton;
    public Button armorButton;

    public GameObject healthLabel;
    public GameObject healthCostLabel;
    public GameObject regenLabel;
    public GameObject regenCostLabel;
    public GameObject armorLabel;
    public GameObject armorCostLabel;
    private MainTowerUpgrades current;
    private CoinWallet wallet;

    private void Awake()
    {
        Instance = this;

        if (healthButton != null) healthButton.onClick.AddListener(() => Purchase(MainTowerStat.MaxHealth));
        if (regenButton != null) regenButton.onClick.AddListener(() => Purchase(MainTowerStat.Regen));
        if (armorButton != null) armorButton.onClick.AddListener(() => Purchase(MainTowerStat.Armor));

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Open(MainTowerUpgrades upgrades)
    {
        ShopUI.Instance?.Close();
        UpgradeUI.Instance?.Close();

        Unsubscribe();

        current = upgrades;
        wallet = CoinWallet.Instance;

        if (current != null)
            current.OnUpgraded += Refresh;
        wallet.OnCoinsChanged += HandleCoinsChanged;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        Unsubscribe();
        current = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Unsubscribe()
    {
        if (current != null)
            current.OnUpgraded -= Refresh;
        if (wallet != null)
            wallet.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int coins)
    {
        Refresh();
    }

    private void Purchase(MainTowerStat stat)
    {
        if (current == null) return;

        current.TryUpgrade(stat);
        Refresh();
    }

    private void Refresh()
    {
        if (current == null) return;

        RefreshButton(healthButton, healthLabel, healthCostLabel, MainTowerStat.MaxHealth, "Max HP");
        RefreshButton(regenButton, regenLabel, regenCostLabel, MainTowerStat.Regen, "Regen");
        RefreshButton(armorButton, armorLabel, armorCostLabel, MainTowerStat.Armor, "Armor");
    }

    private void RefreshButton(Button button, GameObject label, GameObject costLabel, MainTowerStat stat, string title)
    {
        if (button == null) return;

        bool maxed = current.IsMaxed(stat);
        int cost = current.Cost;

        string info = maxed
            ? $"{title}\nMAX"
            : $"{title} {current.GetLevel(stat)}/{current.MaxLevel}\n{current.Describe(stat)}";

        if (label != null && costLabel != null)
        {
            UIText.Set(label, info);
            UIText.Set(costLabel, maxed ? "MAX" : cost.ToString());
        }
        else
        {
            UIText.Set(button.gameObject, maxed ? info : $"{info}\nCost: {cost}");
        }

        button.interactable = !maxed && wallet.CanAfford(cost);
    }
}
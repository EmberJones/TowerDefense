using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    public static UpgradeUI Instance { get; private set; }

    public GameObject panelRoot;

    public GameObject leftButton;
    public GameObject middleButton;
    public GameObject rightButton;
    public GameObject comboButton;

    public Image leftIcon;
    public Image middleIcon;
    public Image rightIcon;
    public Image comboIcon;

    public Text comboButtonLabel;

    private TowerUpgradeTree currentTree;
    private Health currentHealth;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Open(TowerUpgradeTree tree)
    {
        ShopUI.Instance?.Close();

        if (currentHealth != null)
            currentHealth.OnDeath -= HandleTowerDeath;

        currentTree = tree;
        currentHealth = tree != null ? tree.GetComponent<Health>() : null;

        if (currentHealth != null)
            currentHealth.OnDeath += HandleTowerDeath;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshButtons();
    }

    public void Close()
    {
        if (currentHealth != null)
            currentHealth.OnDeath -= HandleTowerDeath;

        currentTree = null;
        currentHealth = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void HandleTowerDeath()
    {
        Close();
    }

    private void RefreshButtons()
    {
        if (currentTree == null) return;

        SetButtonState(leftButton, leftIcon, UpgradeSlot.Left);
        SetButtonState(middleButton, middleIcon, UpgradeSlot.Middle);
        SetButtonState(rightButton, rightIcon, UpgradeSlot.Right);

        UpgradeSlot? pendingCombo = currentTree.GetPendingComboSlot();

        if (comboButton != null)
        {
            bool comboAvailable = pendingCombo.HasValue;
            comboButton.SetActive(comboAvailable);

            if (comboAvailable)
            {
                TowerUpgradeDefinition def = currentTree.FindDefinition(pendingCombo.Value);

                if (comboButtonLabel != null)
                    comboButtonLabel.text = def != null ? def.upgradeName : pendingCombo.Value.ToString();

                if (comboIcon != null && def != null && def.icon != null)
                    comboIcon.sprite = def.icon;
            }
        }
    }

    private void SetButtonState(GameObject buttonObj, Image icon, UpgradeSlot slot)
    {
        if (buttonObj == null) return;

        bool alreadySelected = currentTree.IsSlotSelected(slot);
        buttonObj.SetActive(!alreadySelected);

        if (!alreadySelected && icon != null)
        {
            TowerUpgradeDefinition def = currentTree.FindDefinition(slot);
            if (def != null && def.icon != null)
                icon.sprite = def.icon;
        }
    }

    public void PurchaseUpgrade(string slotName)
    {
        if (currentTree == null) return;

        if (System.Enum.TryParse(slotName, out UpgradeSlot slot))
        {
            currentTree.SelectUpgrade(slot);
        }

        RefreshButtons();
    }

    public void PurchaseCombo()
    {
        if (currentTree == null) return;

        UpgradeSlot? pending = currentTree.GetPendingComboSlot();
        if (pending.HasValue)
            currentTree.SelectUpgrade(pending.Value);

        RefreshButtons();
    }
}
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

        // If two base slots are already chosen, hide ALL base buttons and show combo only.
        // Otherwise, show base buttons (SetButtonState hides the ones already picked).
        int selectedBaseCount = GetSelectedBaseCount();
        bool baseLocked = selectedBaseCount >= 2;

        SetButtonVisibility(leftButton, UpgradeSlot.Left, baseLocked);
        SetButtonVisibility(middleButton, UpgradeSlot.Middle, baseLocked);
        SetButtonVisibility(rightButton, UpgradeSlot.Right, baseLocked);

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
    private void SetButtonVisibility(GameObject buttonObj, UpgradeSlot slot, bool baseLocked)
    {
        if (buttonObj == null) return;

        bool alreadySelected = currentTree.IsSlotSelected(slot);
        buttonObj.SetActive(!alreadySelected && !baseLocked);
    }
    private int GetSelectedBaseCount()
    {
        int count = 0;
        if (currentTree.IsSlotSelected(UpgradeSlot.Left)) count++;
        if (currentTree.IsSlotSelected(UpgradeSlot.Middle)) count++;
        if (currentTree.IsSlotSelected(UpgradeSlot.Right)) count++;
        return count;
    }
    private void SetButtonState(GameObject buttonObj, Image icon, UpgradeSlot slot)
    {
        if (buttonObj == null) return;

        if (icon == null) return;

        TowerUpgradeDefinition def = currentTree.FindDefinition(slot);
        if (def == null)
        {
            Debug.LogWarning($"[UpgradeUI] No definition for slot {slot}.");
            return;
        }
        if (def.icon == null)
        {
            Debug.LogWarning($"[UpgradeUI] Definition '{def.name}' for slot {slot} has no icon assigned.");
            return;
        }

        icon.sprite = def.icon;
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
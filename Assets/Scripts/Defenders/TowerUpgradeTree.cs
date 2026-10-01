using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerUpgradeTree : MonoBehaviour
{
    public List<TowerUpgradeDefinition> availableUpgrades;

    public event Action<TowerUpgradeDefinition> OnUpgradeApplied;

    private HashSet<UpgradeSlot> selectedSlots = new HashSet<UpgradeSlot>();
    private Attacker attacker;
    private Health health;

    private void Awake()
    {
        attacker = GetComponent<Attacker>();
        health = GetComponent<Health>();
    }

    public bool IsSlotSelected(UpgradeSlot slot)
    {
        return selectedSlots.Contains(slot);
    }

    public bool CanSelect(UpgradeSlot slot)
    {
        if (selectedSlots.Contains(slot)) return false;

        if (slot == UpgradeSlot.Left || slot == UpgradeSlot.Middle || slot == UpgradeSlot.Right)
            return selectedSlots.Count < 2;

        return GetPendingComboSlot() == slot;
    }

    public UpgradeSlot? GetPendingComboSlot()
    {
        if (selectedSlots.Contains(UpgradeSlot.Left) && selectedSlots.Contains(UpgradeSlot.Middle) && !selectedSlots.Contains(UpgradeSlot.LeftMiddle))
            return UpgradeSlot.LeftMiddle;
        if (selectedSlots.Contains(UpgradeSlot.Left) && selectedSlots.Contains(UpgradeSlot.Right) && !selectedSlots.Contains(UpgradeSlot.LeftRight))
            return UpgradeSlot.LeftRight;
        if (selectedSlots.Contains(UpgradeSlot.Middle) && selectedSlots.Contains(UpgradeSlot.Right) && !selectedSlots.Contains(UpgradeSlot.MiddleRight))
            return UpgradeSlot.MiddleRight;

        return null;
    }

    public void SelectUpgrade(UpgradeSlot slot)
    {
        if (!CanSelect(slot)) return;

        selectedSlots.Add(slot);
        ApplyUpgrade(slot);
    }

    private void ApplyUpgrade(UpgradeSlot slot)
    {
        TowerUpgradeDefinition def = FindDefinition(slot);
        if (def == null) return;

        if (attacker != null)
        {
            attacker.attackDamage += def.bonusDamage;
            attacker.attackRange += def.bonusRange;
            attacker.attackInterval *= def.attackIntervalMultiplier;
        }

        if (health != null && def.bonusMaxHealth != 0)
        {
            health.MaxHealth += def.bonusMaxHealth;
            health.CurrentHealth += def.bonusMaxHealth;
        }

        if (def.effectPrefab != null)
        {
            GameObject effectObj = Instantiate(def.effectPrefab, transform);
            effectObj.transform.localPosition = Vector3.zero;
        }

        OnUpgradeApplied?.Invoke(def);
    }

    public TowerUpgradeDefinition FindDefinition(UpgradeSlot slot)
    {
        foreach (var def in availableUpgrades)
        {
            if (def.slot == slot)
                return def;
        }
        return null;
    }
}
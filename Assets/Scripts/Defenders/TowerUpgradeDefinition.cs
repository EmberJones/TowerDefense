using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Upgrade")]
public class TowerUpgradeDefinition : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;
    public UpgradeSlot slot;
    public Sprite icon;

    public int bonusDamage;
    public float bonusRange;
    public float attackIntervalMultiplier = 1f;
    public int bonusMaxHealth;

    public GameObject effectPrefab;
}
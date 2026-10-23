using UnityEngine;

public class ThornsStatus : MonoBehaviour
{
    private int damagePerThorn;
    private int thornAmount;
    private IDamageable damageable;
    public int DamagePerThorn => damagePerThorn;
    public int ThornAmount => thornAmount;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    public void ApplyThorns(int damagePerThorn, int thornAmount)
    {
        this.damagePerThorn = damagePerThorn;
        this.thornAmount += thornAmount;
        damageable?.TakeDamage(this.thornAmount * this.damagePerThorn);
    }
}
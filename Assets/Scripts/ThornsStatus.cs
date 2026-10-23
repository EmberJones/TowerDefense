using UnityEngine;

public class ThornsStatus : MonoBehaviour
{
    private int damagePerThorn;
    private int thornAmount;
    [SerializeField] bool debug = false;
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
        if(debug)
            Debug.Log($"[ThornsStatus] Applied thorns: dpt={this.damagePerThorn}, {this.thornAmount} thorns,{this.thornAmount * this.damagePerThorn} damage");
    }
}
using UnityEngine;

public class ThornsStatus : MonoBehaviour
{
    public float tickInterval = 1f;
    [SerializeField] bool debug = false;

    private int damagePerThorn;
    private int thornAmount;
    private float tickTimer;
    private IDamageable damageable;

    public int DamagePerThorn => damagePerThorn;
    public int ThornAmount => thornAmount;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    public void ApplyThorns(int damagePerThorn, int thornAmount)
    {
        this.damagePerThorn = Mathf.Max(this.damagePerThorn, damagePerThorn);
        this.thornAmount += thornAmount;
    }

    private void Update()
    {
        if (thornAmount <= 0)
        {
            Destroy(this);
            return;
        }

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;

        int damage = thornAmount * damagePerThorn;
        damageable?.TakeDamage(damage);
        if (debug) Debug.Log($"[ThornsStatus] tick: {thornAmount} thorns, {damage} damage");

        thornAmount--;
        tickTimer = tickInterval;
    }
}
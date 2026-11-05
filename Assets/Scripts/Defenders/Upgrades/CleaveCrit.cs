using UnityEngine;

public class CleaveCrit : MonoBehaviour
{
    private void Awake()
    {
        Attacker attacker = GetComponentInParent<Attacker>();
        if (attacker == null) return;

        CleaveOnHitEffect cleave = attacker.GetComponentInChildren<CleaveOnHitEffect>();
        Crit crit = attacker.GetComponentInChildren<Crit>();

        if (cleave == null || crit == null) return;

        cleave.crit = crit;
        cleave.canCrit = true;
    }
}
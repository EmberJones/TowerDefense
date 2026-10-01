using UnityEngine;

public class LightningBurnComboEffect : MonoBehaviour
{
    public int burnDamagePerTick = 2;
    public float tickInterval = 1f;
    public float burnDuration = 3f;

    private ChainLightningEffect chainLightning;

    private void Awake()
    {
        chainLightning = transform.parent != null
            ? transform.parent.GetComponentInChildren<ChainLightningEffect>()
            : null;

        if (chainLightning != null)
            chainLightning.OnChainJump += HandleChainJump;
    }

    private void OnDestroy()
    {
        if (chainLightning != null)
            chainLightning.OnChainJump -= HandleChainJump;
    }

    private void HandleChainJump(Transform target)
    {
        if (target == null) return;

        BurnStatus burn = target.GetComponent<BurnStatus>();
        if (burn == null)
            burn = target.gameObject.AddComponent<BurnStatus>();

        burn.ApplyBurn(burnDamagePerTick, tickInterval, burnDuration);
    }
}
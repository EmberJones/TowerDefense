using UnityEngine;

public class ChainSlowComboEffect : MonoBehaviour
{
    [Range(0f, 0.8f)] public float slowFraction = 0.3f;
    public float duration = 2f;

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

        SlowStatus.Apply(target.gameObject, this, slowFraction, duration);
        QuickVFX.Burst(QuickVFX.Center(target), new Color(0.4f, 0.8f, 1f), 8, 2f, 0.12f, 0.4f);
    }
}
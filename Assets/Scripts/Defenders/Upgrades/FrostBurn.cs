using UnityEngine;

public class BurnSlowComboEffect : MonoBehaviour
{
    [Range(0f, 0.8f)] public float startSlow = 0.4f;
    public float slowPerSecond = 0.1f;
    [Range(0f, 0.8f)] public float maxSlow = 0.75f;

    private Attacker attacker;

    private void Awake()
    {
        attacker = GetComponentInParent<Attacker>();
        if (attacker != null)
            attacker.OnAttackHit += HandleHit;
    }

    private void OnDestroy()
    {
        if (attacker != null)
            attacker.OnAttackHit -= HandleHit;
    }

    private void HandleHit(Transform target)
    {
        if (target == null) return;

        BurnSlowStatus status = target.GetComponent<BurnSlowStatus>();
        if (status == null)
            status = target.gameObject.AddComponent<BurnSlowStatus>();

        status.Configure(startSlow, slowPerSecond, maxSlow);
    }
}

public class BurnSlowStatus : MonoBehaviour
{
    private const float Grace = 0.3f;
    private const float Linger = 1f;

    private float startSlow;
    private float slowPerSecond;
    private float maxSlow;
    private float burningTime;
    private float graceTimer = Grace;
    private BurnStatus burn;

    public void Configure(float start, float perSecond, float max)
    {
        startSlow = Mathf.Max(startSlow, start);
        slowPerSecond = Mathf.Max(slowPerSecond, perSecond);
        maxSlow = Mathf.Max(maxSlow, max);
    }

    private void Update()
    {
        if (burn == null) burn = GetComponent<BurnStatus>();

        if (burn == null)
        {
            graceTimer -= Time.deltaTime;
            if (graceTimer <= 0f) Destroy(this);
            return;
        }

        graceTimer = Grace;
        burningTime += Time.deltaTime;

        float slow = Mathf.Min(maxSlow, startSlow + burningTime * slowPerSecond);
        SlowStatus.Apply(gameObject, this, slow, Linger);
    }
}
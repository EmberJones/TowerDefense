using UnityEngine;

[RequireComponent(typeof(TowerUpgradeTree))]
public class DefenderUpgradeVisuals : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeMaterial
    {
        public TowerUpgradeDefinition upgrade;
        public Material material;
    }

    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private UpgradeMaterial[] upgradeMaterials;

    private TowerUpgradeTree tree;

    private void Reset() { targetRenderer = FindRenderer(); }

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = FindRenderer();
        tree = GetComponent<TowerUpgradeTree>();
        tree.OnUpgradeApplied += HandleUpgradeApplied;
    }

    private void OnDestroy()
    {
        if (tree != null) tree.OnUpgradeApplied -= HandleUpgradeApplied;
    }

    private Renderer FindRenderer()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null && !(r is LineRenderer)) return r;
        r = GetComponentInChildren<SkinnedMeshRenderer>();
        if (r != null) return r;
        return GetComponentInChildren<MeshRenderer>();
    }

    private void HandleUpgradeApplied(TowerUpgradeDefinition def)
    {
        if (targetRenderer == null || upgradeMaterials == null) return;

        foreach (UpgradeMaterial entry in upgradeMaterials)
        {
            if (entry == null || entry.upgrade != def || entry.material == null) continue;

            Material[] mats = targetRenderer.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= mats.Length)
            {
                Debug.LogWarning($"{name}: material index {materialIndex} is out of range ({mats.Length} slots).", this);
                return;
            }

            mats[materialIndex] = entry.material;
            targetRenderer.sharedMaterials = mats;
            return;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class WaveTimerUI : MonoBehaviour
{
    public WaveManager waveManager;
    public Image fill;
    public GameObject label;

    [Header("Fill colour")]
    public Color startColor = new Color(1f, 0.9f, 0.2f);
    public Color endColor = new Color(1f, 0.45f, 0.05f);

    [Header("Soft edge")]
    public Image[] featherLayers;
    public float featherSize = 0.02f;
    [Range(0f, 1f)] public float featherAlpha = 0.35f;

    private void Update()
    {
        if (waveManager == null) return;

        bool counting = waveManager.IsCountingDown && waveManager.CountdownDuration > 0f;
        float progress = counting ? 1f - waveManager.CountdownRemaining / waveManager.CountdownDuration : 0f;
        Color color = Color.Lerp(startColor, endColor, progress);

        int layers = featherLayers != null ? featherLayers.Length : 0;
        float span = layers * featherSize;
        float extended = progress * (1f + span);

        if (fill != null)
        {
            fill.fillAmount = Mathf.Clamp01(extended - span);
            fill.color = color;
        }

        for (int i = 0; i < layers; i++)
        {
            if (featherLayers[i] == null) continue;

            featherLayers[i].fillAmount = Mathf.Clamp01(extended - i * featherSize);
            featherLayers[i].color = new Color(color.r, color.g, color.b, featherAlpha);
        }

        string text;
        if (counting)
        {
            string prefix = waveManager.WaveNumber == 0 ? "First wave in " : "Next wave in ";
            text = prefix + Mathf.CeilToInt(waveManager.CountdownRemaining) + "s";
        }
        else
        {
            text = "Wave " + waveManager.WaveNumber;
        }

        UIText.Set(label, text);
    }
}
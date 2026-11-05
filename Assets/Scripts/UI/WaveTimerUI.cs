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

    private void Update()
    {
        if (waveManager == null) return;

        bool counting = waveManager.IsCountingDown && waveManager.CountdownDuration > 0f;
        float progress = counting ? 1f - waveManager.CountdownRemaining / waveManager.CountdownDuration : 0f;

        if (fill != null)
        {
            fill.fillAmount = progress;
            fill.color = Color.Lerp(startColor, endColor, progress);
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
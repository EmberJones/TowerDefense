using UnityEngine;
using UnityEngine.UI;

public class WaveTimerUI : MonoBehaviour
{
    public WaveManager waveManager;
    public Image fill;
    public GameObject label;

    private void Update()
    {
        if (waveManager == null) return;

        bool counting = waveManager.IsCountingDown && waveManager.CountdownDuration > 0f;

        if (fill != null)
            fill.fillAmount = counting ? 1f - waveManager.CountdownRemaining / waveManager.CountdownDuration : 0f;

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
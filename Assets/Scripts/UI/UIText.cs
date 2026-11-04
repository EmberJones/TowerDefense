using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIText
{
    public static void Set(GameObject target, string value)
    {
        if (target == null) return;

        Text legacy = target.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = value;
            return;
        }

        TMP_Text tmp = target.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = value;
    }
}
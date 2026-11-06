using System.Collections;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    public float minDuration = 2.5f;
    public int minFrames = 5;
    public GameObject[] alsoHide;

    private IEnumerator Start()
    {
        float elapsed = 0f;
        int frames = 0;
        while (elapsed < minDuration || frames < minFrames)
        {
            Time.timeScale = 0f;
            elapsed += Time.unscaledDeltaTime;
            frames++;
            yield return null;
        }

        Time.timeScale = 1f;

        foreach (GameObject extra in alsoHide)
            if (extra != null) extra.SetActive(false);

        gameObject.SetActive(false);
    }
}
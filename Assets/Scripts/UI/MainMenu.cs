using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene to load when Play is pressed")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;


    [SerializeField] private GameObject loadingScreen;

    private bool loading;
    private void Start()
    {

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        if (loading) return;
        loading = true;
        StartCoroutine(LoadGame());
    }
    private IEnumerator LoadGame()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        yield return null;
        yield return null;

        SceneManager.LoadSceneAsync(gameSceneName);
    }
    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
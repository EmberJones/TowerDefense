using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Tower Health")]
    [SerializeField] private Health towerHealth;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (towerHealth != null)
            towerHealth.OnDeath += ShowGameOver;
    }

    private void OnDestroy()
    {
        if (towerHealth != null)
            towerHealth.OnDeath -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // Hook this up to the "Play Again" button's OnClick()
    public void PlayAgain()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Hook this up to the "Main Menu" button's OnClick()
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
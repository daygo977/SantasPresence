using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public GameObject gameOverMenu;

    public MonoBehaviour cameraController;
    public GameObject pauseMenu;

    public bool isGameOver = false;

    void Awake()
    {
        isGameOver = false;
    }

    void Start()
    {
        gameOverMenu.SetActive(false);
    }

    public void GameOverLock()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        gameOverMenu.SetActive(true);

        //Freeze time
        Time.timeScale = 0f;

        //Disable pause logic so ESC can't open/close pause menu
        if (pauseMenu != null)
        {
            PauseMenu.isPaused = false;
            pauseMenu.SetActive(false);
        }

        //Lock camera movement
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        //Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Quit()
    {
        Application.Quit();
    }
}

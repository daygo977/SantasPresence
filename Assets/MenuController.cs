using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pauseMenu;
    public GameObject optionsMenu;
    public GameObject winMenu;
    public GameObject loseMenu;

    private enum MenuState
    {
        None,
        Pause,
        Win,
        Lose
    }

    private MenuState currentState = MenuState.None;

    void Start()
    {
        CloseAllMenus();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
        }
    }

    private void HandleEscape()
    {
        // ESC does nothing during win/lose
        if (currentState == MenuState.Win || currentState == MenuState.Lose)
            return;

        // If options is open, go back to pause
        if (optionsMenu.activeSelf)
        {
            ShowPause();
            return;
        }

        // Toggle pause
        if (currentState == MenuState.Pause)
            ResumeGame();
        else
            ShowPause();
    }

    public void ShowPause()
    {
        CloseAllMenus();
        pauseMenu.SetActive(true);
        currentState = MenuState.Pause;
        GameStateManager.Instance.LockGame();
    }

    public void ShowOptions()
    {
        CloseAllMenus();
        optionsMenu.SetActive(true);
        GameStateManager.Instance.LockGame();
    }

    public void ShowWin()
    {
        CloseAllMenus();
        winMenu.SetActive(true);
        currentState = MenuState.Win;
        GameStateManager.Instance.LockGame();
    }

    public void ShowLose()
    {
        CloseAllMenus();
        loseMenu.SetActive(true);
        currentState = MenuState.Lose;
        GameStateManager.Instance.LockGame();
    }

    public void ResumeGame()
    {
        CloseAllMenus();
        currentState = MenuState.None;
        GameStateManager.Instance.UnlockGame();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void CloseAllMenus()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        winMenu.SetActive(false);
        loseMenu.SetActive(false);
    }
}

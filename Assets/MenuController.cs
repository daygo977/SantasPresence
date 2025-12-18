using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pauseMenu;
    public GameObject optionsMenu;
    public GameObject winMenu;
    public GameObject loseMenu;

    public GameObject gameHud;

    [Header("Win UI")]
    public TextMeshProUGUI newBestText;

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

        if (gameHud != null)
            gameHud.SetActive(true);

        currentState = MenuState.None;
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

        if (newBestText != null && GameManager.Instance != null)
        {
            newBestText.gameObject.SetActive(GameManager.Instance.isNewBest);
        }
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

        if (gameHud != null)
            gameHud.SetActive(true);

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
        if (UnityEditor.EditorApplication.isPlaying == true)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
    }

    private void CloseAllMenus()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        winMenu.SetActive(false);
        loseMenu.SetActive(false);

        if (gameHud != null)
            gameHud.SetActive(false);
    }

}

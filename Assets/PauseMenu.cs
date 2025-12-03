using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject optionsMenu;

    public static bool isPaused = false;

    void Awake()
    {
        // Always reset when entering play mode
        isPaused = false;
        Time.timeScale = 1f; // Time should be unpaused by default
    }

    void Start()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If settings is open, close it and go back to pause menu
            if (optionsMenu.activeInHierarchy)
            {
                optionsMenu.SetActive(false);
                pauseMenu.SetActive(true);
                return;
            }

            // If game is paused and not in settings, resume game
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                // If not paused, pause it
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("Paused");
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void RestartGame()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenOptions()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu"); // CHANGE THIS (THIS IS AN OLD NAME!)
    }
}

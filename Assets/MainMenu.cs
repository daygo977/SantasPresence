using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private float delayTime = 1f;       // Default delay time
    public GameObject QuitYesOrNoUI;    // A prompt that asks if player is sure to quit the game
    public GameObject MainMenuUI;       // Main Menu UI
    public GameObject optionsMenu;      // Options menu for enabling/disabling
    public GameObject UIBlocker;        // Panel that is raycastable so it blocks button from being able to be pressed
    public FadeIn fadeTransition;       // NEED TO ADD
    public MenuMusic menuMusic;         // NEED TO ADD

    public void PlayGame()
    {
        // When player clicks PLAY, head into next scene with a slight delay, allowing for fade transition and music to fade
        StartCoroutine(DelayStart());
    }

    public void Options()
    {
        // If player clicks on options button
        optionsMenu.SetActive(true);
        MainMenuUI.SetActive(false);
    }

    public void BackToPause()
    {
        // Coming back from options to main menu
        optionsMenu.SetActive(false);
        MainMenuUI.SetActive(true);
    }

    public void QuitPanelOn()
    {
        // Asks if player is sure they want to quit the game, also activates UIBlocker to prevent them from being able to click Play, Options, or Quit during this part.
        QuitYesOrNoUI.SetActive(true);
        UIBlocker.SetActive(true);
    }

    // If player clicks YES, confirming to quit, quit game
    public void Quit()
    {
        StartCoroutine(DelayQuit());
    }

    // If player does not want to quit by clicking NO, deactivate UIBlocker and return back to main menu
    public void QuitCancel()
    {
        QuitYesOrNoUI.SetActive(false);
        UIBlocker.SetActive(false);
    }

    private IEnumerator DelayStart()
    {
        if (menuMusic != null && fadeTransition != null)
        {
            // Prevent player from being able to press buttons during transition to next scene
            UIBlocker.SetActive(true);
            // Start both music fading and fade transition at the same time
            StartCoroutine(menuMusic.FadeOut());
            StartCoroutine(fadeTransition.FadeInImage());

            // Wait for whichever takes longer, either music fade duration or fade transition
            yield return new WaitForSecondsRealtime(Mathf.Max(menuMusic.fadeDuration, fadeTransition.fadeDuration));
        }
        else
        {
            // Only if music and fade is not assigned
            yield return new WaitForSecondsRealtime(delayTime);
        }

        // Disable UIBlocker, in case it affects later scenes
        UIBlocker.SetActive(false);
        // Load the first level
        SceneManager.LoadSceneAsync("Apartment"); // TEMPORARY // TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY// TEMPORARY
    }

    private IEnumerator DelayQuit()
    {
        // If player is sure to quit, wait a second (mainly to allow click sfx to play)
        // Quit the game for player if they are either on application or editor
        yield return new WaitForSecondsRealtime(delayTime);
        if (UnityEditor.EditorApplication.isPlaying == true)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
    }
}

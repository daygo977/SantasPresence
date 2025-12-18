using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Progress")]
    public int giftsPlanted = 0;
    public int totalTrees = 0;

    [Header("UI")]
    public TextMeshProUGUI giftsText;
    public GameObject gameHud;
    public TextMeshProUGUI hudTimerText;
    public TextMeshProUGUI finalTimeText;

    public TextMeshProUGUI escapeText;
    public Color escapeReadyColor = Color.green;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip cheeringSFX;
    public AudioClip hohohoSFX;

    [Header("Escape Triggers")]
    public List<escapeTrigger> escapeTriggers = new List<escapeTrigger>(); // List of escape triggers as there may be more than 1 in a level (like level 2)

    private float levelTimer = 0f;
    private bool timerRunning = false;
    private bool escapeUnlocked = false;
    private bool hasWon = false;
    private bool loseHandled = false;

    [HideInInspector] public bool isNewBest = false;
    [HideInInspector] public bool lostGame = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameHud.SetActive(true);

        if (escapeText != null)
        {
            Color c = escapeText.color;
            c.a = 0f;
            escapeText.color = c;
        }
    }

    private void Start()
    {
        UpdateGiftText(); // Update the tree count to be 0/n at start
        timerRunning = true;
    }

    private void Update()
    {
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
            hudTimerText.text = FormatTime(levelTimer); // Continuously update the timer
        }

        if (lostGame)
            LoseGame();
    }

    public void RegisterTree()
    {
        // Count amount of trees in the level
        totalTrees++;
        UpdateGiftText();
    }

    public void PlantGift()
    {
        giftsPlanted++;

        if (!escapeUnlocked && giftsPlanted >= totalTrees)
        {
            escapeUnlocked = true;
            EnableAllEscapes();
            ShowEscapeUI();
        }

        UpdateGiftText();
    }

    private void EnableAllEscapes()
    {
        foreach (var trigger in escapeTriggers)
        {
            if (trigger != null)
                trigger.EnableEscape();
        }
    }

    private void ShowEscapeUI()
    {
        if (!escapeText) return;

        Color c = escapeText.color;
        c.a = 1f;
        escapeText.color = c;
    }

    private void UpdateGiftText()
    {
        giftsText.text = $"Gifts Planted: {giftsPlanted}/{totalTrees}";

        if (escapeUnlocked)
            giftsText.color = escapeReadyColor;
    }

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;
        return $"{minutes:0}:{seconds:00.00}";
    }

    public void WinGame()
    {
        if (hasWon)
            return;

        hasWon = true;
        timerRunning = false;

        finalTimeText.text = FormatTime(levelTimer);
        SaveBestTime();

        if (sfxSource)
        {
            if (cheeringSFX)
                sfxSource.PlayOneShot(cheeringSFX, 0.2f);
            if (hohohoSFX)
                sfxSource.PlayOneShot(hohohoSFX, 0.2f);
        }

        FindObjectOfType<MenuController>().ShowWin();
    }

    private void LoseGame()
    {
        if (loseHandled)
            return;

        loseHandled = true;
        FindObjectOfType<MenuController>().ShowLose();
    }

    private void SaveBestTime()
    {
        string levelKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string prefKey = $"BestTime_{levelKey}";

        float previousBest = PlayerPrefs.GetFloat(prefKey, float.MaxValue);

        if (levelTimer < previousBest)
        {
            isNewBest = true;
            PlayerPrefs.SetFloat(prefKey, levelTimer);
            PlayerPrefs.Save();
        }
        else
        {
            isNewBest = false;
        }
    }
}

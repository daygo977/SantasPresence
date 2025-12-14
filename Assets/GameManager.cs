using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Progress")]
    public int giftsPlanted = 0;
    public int totalTrees = 0;

    [Header("UI")]
    public TextMeshProUGUI giftsText;
    public GameObject gameHud;
    public GameObject gameWinPanel;
    public GameObject gameOverPanel;

    public TextMeshProUGUI hudTimerText;
    public TextMeshProUGUI finalTimeText;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip cheeringSFX;
    public AudioClip hohohoSFX;

    private float levelTimer = 0f;
    private bool timerRunning = false;

    [HideInInspector]
    public bool lostGame = false;

    [Header("Escape")]
    public EscapeTrigger escapeTrigger;
    private bool escapeUnlocked = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameWinPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        gameHud.SetActive(true);
    }

    private void Start()
    {
        UpdateGiftText();
        timerRunning = true;
    }

    private void Update()
    {
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
            hudTimerText.text = FormatTime(levelTimer);
        }

        if (lostGame)
        {
            LoseGame();
        }
    }

    public void RegisterTree()
    {
        totalTrees++;
        UpdateGiftText();
    }

    public void PlantGift()
    {
        giftsPlanted++;
        UpdateGiftText();

        if (!escapeUnlocked && giftsPlanted >= totalTrees)
        {
            escapeUnlocked = true;
            CueEscape();
        }
    }


    private void UpdateGiftText()
    {
        giftsText.text = $"Gifts Planted: {giftsPlanted}/{totalTrees}";
    }

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;
        return string.Format("{0:0}:{1:00.00}", minutes, seconds);
    }

    private void CueEscape()
    {
        Debug.Log("All gifts planted! Escape unlocked.");

        if (escapeTrigger != null)
        {
            escapeTrigger.EnableEscape();
        }
    }

    private void WinGame()
    {
        gameWinPanel.SetActive(true);
        gameHud.SetActive(false);
        Time.timeScale = 0f;
        timerRunning = false;

        finalTimeText.text = FormatTime(levelTimer);

        if (sfxSource)
        {
            if (cheeringSFX)
                sfxSource.PlayOneShot(cheeringSFX, 0.2f);
            if (hohohoSFX)
                sfxSource.PlayOneShot(hohohoSFX, 0.2f);
        }
    }

    private void LoseGame()
    {
        if (!gameOverPanel.activeSelf)
        {
            gameHud.SetActive(false);
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}

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

    public TextMeshProUGUI hudTimerText;
    public TextMeshProUGUI finalTimeText;

    public TextMeshProUGUI escapeText;
    public Color escapeReadyColor = Color.green;


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

    private bool loseHandled = false;

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

        if (!escapeUnlocked && giftsPlanted >= totalTrees)
        {
            escapeUnlocked = true;
            CueEscape();
            ShowEscapeUI();
        }

        UpdateGiftText();
    }

    private void UpdateGiftText()
    {
        giftsText.text = $"Gifts Planted: {giftsPlanted}/{totalTrees}";

        if (escapeUnlocked)
        {
            giftsText.color = escapeReadyColor;
        }
    }

    private void ShowEscapeUI()
    {
        if (escapeText == null)
            return;

        Color c = escapeText.color;
        c.a = 1f;
        escapeText.color = c;
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
        timerRunning = false;
        finalTimeText.text = FormatTime(levelTimer);

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
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int giftsPlanted = 0; // Amount of gifts successfully dropped off at a tree (initially 0 at start of level)
    public int totalTrees = 0; // Amount of trees in the current scene/level

    public TextMeshProUGUI giftsText; // TMP Text placeholder

    public GameObject gameHud;      // HUD
    public GameObject gameWinPanel; // Game win screen

    public TextMeshProUGUI hudTimerText;        // HUD timer (updates)
    public TextMeshProUGUI finalTimeText;    // Result time for win screen

    private float levelTimer = 0f;  // Initiially timer starts at 0 seconds
    private bool timerRunning = false;  // Initially timer has not started yet until first frame

    public AudioSource sfxSource;   // SFX Mixer for universal volume/effects control
    public AudioClip cheeringSFX; // Win Condition SFX
    public AudioClip hohohoSFX; // Win Condition SFX

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameWinPanel.SetActive(false);  // Initially false
        gameHud.SetActive(true);        // Initially HUD is active
    }

    void Update()
    {
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
            hudTimerText.text = FormatTime(levelTimer);
        }
    }

    void Start()
    {
        UpdateGiftText();   // Update counter for HUD
        timerRunning = true;    // Timer begins
    }

    public void RegisterTree()
    {
        totalTrees++;       // Update counter of trees read in the level
        UpdateGiftText();   // Determine denominator to counter for HUD
    }

    public void PlantGift()
    {
        giftsPlanted++;     // Successfully planted a gift, update counter
        UpdateGiftText();   // Update text for HUD

        if (giftsPlanted >= totalTrees) // Checks every time a gift is planted that if the the counter is n/n, then player wins the game
        {
            WinGame(); // Game win function call
        }
    }

    private void UpdateGiftText()
    {
        giftsText.text = $"Gifts Planted: {giftsPlanted}/{totalTrees}"; // Updating the placeholder text to "Gifts planted: " + n/n
    }

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t % 60f;

        return string.Format("{00:0}:{1:00.00}", minutes, seconds); // Formatting using minutes and seconds
    }

    private void WinGame()
    {
        Debug.Log("Player has won the game");
        gameWinPanel.SetActive(true);   // Show win screen
        gameHud.SetActive(false);
        Time.timeScale = 0f;            // Pause time

        timerRunning = false;           // Stop timer
        finalTimeText.text = FormatTime(levelTimer);    // Time formatting function is called

        // If SFX Manager source is existing and connected...
        if (sfxSource)
        {
            // Play sound effects if they are existing and connected
            if (cheeringSFX)
                sfxSource.PlayOneShot(cheeringSFX, 0.2f);
            if (hohohoSFX)
                sfxSource.PlayOneShot(hohohoSFX, 0.2f);
        }
    }
}

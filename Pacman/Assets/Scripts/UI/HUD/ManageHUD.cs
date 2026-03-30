using UnityEngine;
using TMPro;

public class ManageHUD : MonoBehaviour
{
    public static ManageHUD Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    [Header("Win Screen")]
    public GameObject winPanel;

    [Header("Lose Screen")]
    public GameObject losePanel; // Drag your Lose Canvas/Panel here

    [Header("Timer")]
    public TextMeshProUGUI timerText; // Drag your Timer Text here

    public void ShowLoseScreen()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void SetTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            // Ensure time doesn't go below zero
            float displayTime = Mathf.Max(0, timeRemaining);

            // Calculate minutes and seconds
            int minutes = Mathf.FloorToInt(displayTime / 60);
            int seconds = Mathf.FloorToInt(displayTime % 60);

            // Format as 00:00
            timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            // Optional: Change color to red when under 10 seconds
            if (timeRemaining <= 10f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    private void Awake()
    {
        // Force this instance to be THE instance as soon as the level loads
        Instance = this;

        // Safety check: if the game is paused from a previous win/loss, unpause it
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        // These MUST match the name of your player script (testMove)
        testMove.OnScoreChanged += UpdateScoreUI;
        testMove.OnLivesChanged += UpdateLivesUI;
    }

    private void OnDisable()
    {
        testMove.OnScoreChanged -= UpdateScoreUI;
        testMove.OnLivesChanged -= UpdateLivesUI;
    }

    // This keeps GameManager happy
    public void InitHUD(int lives, int level, float timer, bool fruit)
    {
        UpdateLivesUI(lives);
    }

    public void UpdateScoreUI(int newScore)
    {
        if (scoreText != null)
            scoreText.text = "SCORE: " + newScore.ToString("D6");
    }

    public void UpdateLivesUI(int currentLives)
    {
        if (livesText != null)
            livesText.text = "LIVES: " + currentLives.ToString();
    }

    public void ShowWinScreen()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

}
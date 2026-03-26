using UnityEngine;
using TMPro;

public class ManageHUD : MonoBehaviour
{
    public static ManageHUD Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    [Header("Win Screen")]
    public GameObject winPanel; // Drag your WinPanel here in the Inspector

    public void ShowWinScreen()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            // Optional: Stop time so ghosts stop moving
            Time.timeScale = 0f;
        }
    }

    private void Awake()
    {
        // Singleton setup so GameManager can find it
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to both events from your movement script
        testMove.OnScoreChanged += UpdateScoreUI;
        testMove.OnLivesChanged += UpdateLivesUI;
    }

    private void OnDisable()
    {
        testMove.OnScoreChanged -= UpdateScoreUI;
        testMove.OnLivesChanged -= UpdateLivesUI;
    }

    // This matches the InitHUD call in your GameManager
    public void InitHUD(int lives, int level, float timer, bool fruit)
    {
        UpdateLivesUI(lives);
        UpdateScoreUI(PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.Points : 0);
        // Add timer/level initialization here if needed later
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

    // Add this to your ManageHUD variables
    public TextMeshProUGUI timerText;

    // Add this function inside ManageHUD
    public void SetTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            // CeilToInt turns 9.2 into 10 so the player doesn't see decimals
            timerText.text = "TIME: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }
}
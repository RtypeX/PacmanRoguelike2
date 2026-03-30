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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    // Dummy method so GameManager doesn't break
    public void SetTimerDisplay(float time) { }
}
using UnityEngine;
using TMPro;

public class ScoreHUD : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        // Unsubscribe first to prevent double-counting bugs
        testMove.OnScoreChanged -= UpdateScoreDisplay;
        testMove.OnScoreChanged += UpdateScoreDisplay;
    }

    private void OnDisable()
    {
        testMove.OnScoreChanged -= UpdateScoreDisplay;
    }

    private void Start()
    {
        // Set initial score based on what's in the PlayerUpgrades "Wallet"
        if (PlayerUpgrades.Instance != null)
        {
            UpdateScoreDisplay(PlayerUpgrades.Instance.Points);
        }
        else
        {
            UpdateScoreDisplay(0);
        }
    }

    // This method handles the actual text update
    private void UpdateScoreDisplay(int currentScore)
    {
        if (scoreText != null)
        {
            // Simple text: Score: 10, Score: 20, etc.
            scoreText.text = "SCORE: " + currentScore.ToString("D6");
        }
    }
}
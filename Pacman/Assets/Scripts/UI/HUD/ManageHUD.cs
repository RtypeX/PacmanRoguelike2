using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Added this for menu loading

public class ManageHUD : MonoBehaviour
{
    public static ManageHUD Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;

    [Header("Win Screen")]
    public GameObject winPanel;

    [Header("Lose Screen")]
    public GameObject losePanel;
    public Button loseMenuButton; // Drag the "Menu" button from your Lose Panel here!

    [Header("Timer")]
    public TextMeshProUGUI timerText;
    private float powerUpMaxDuration = 8f;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu"; // Change this to match your menu scene name

    public void ShowLoseScreen()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Time.timeScale = 0f;

            // Link the button logic
            if (loseMenuButton != null)
            {
                loseMenuButton.onClick.RemoveAllListeners();
                loseMenuButton.onClick.AddListener(ReturnToMenu);
            }
        }
    }

    // Logic to clean up and go back to the menu
    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // IMPORTANT: Unpause the game before leaving!
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            float displayTime = Mathf.Max(0, timeRemaining);
            int minutes = Mathf.FloorToInt(displayTime / 60);
            int seconds = Mathf.FloorToInt(displayTime % 60);
            timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            if (timeRemaining <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        testMove.OnScoreChanged += UpdateScoreUI;
        testMove.OnLivesChanged += UpdateLivesUI;
    }

    private void OnDisable()
    {
        testMove.OnScoreChanged -= UpdateScoreUI;
        testMove.OnLivesChanged -= UpdateLivesUI;
    }

    public void InitHUD(int lives, int level, float timer, bool fruit)
    {
        UpdateLivesUI(lives);
        SetTimerDisplay(timer);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
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

            Button continueButton = GetWinPanelButton("NextLevel");
            if (continueButton == null)
                continueButton = winPanel.GetComponentInChildren<Button>(true);

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.ProceedToUpgrades();
                });
            }
        }
    }

    private Button GetWinPanelButton(string buttonName)
    {
        if (winPanel == null)
            return null;

        Button[] buttons = winPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == buttonName)
                return button;
        }

        return null;
    }

    public void ShowScorePopup(int amount, Vector3 worldPosition) { }
    public void UpdateFruitCurrency(int amount) { }
    public void SetPowerUpMaxDuration(float duration) => powerUpMaxDuration = duration;
}

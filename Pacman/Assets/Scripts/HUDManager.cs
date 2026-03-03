using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUDManager - Controls all in-game UI elements.
/// Attach to a Canvas GameObject in your game scene.
///
/// REQUIRED UI CHILDREN (create these under your Canvas):
///   - ScoreText          (TextMeshProUGUI)
///   - HighScoreText      (TextMeshProUGUI)
///   - LevelText          (TextMeshProUGUI)
///   - TimerText          (TextMeshProUGUI)
///   - FruitCurrencyText  (TextMeshProUGUI)
///   - LivesContainer     (GameObject - parent for heart icons)
///   - HeartPrefab        (Image prefab assigned in inspector)
///   - PowerUpBar         (Slider)
///   - PowerUpBarGroup    (GameObject - parent to show/hide entire bar)
///   - ScorePopupPrefab   (TextMeshProUGUI prefab for floating score popups)
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Score")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Level & Timer")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI timerText;

    [Header("Fruit Currency")]
    public TextMeshProUGUI fruitCurrencyText;

    [Header("Lives")]
    public Transform livesContainer;
    public GameObject heartPrefab;

    [Header("Power-Up Bar")]
    public GameObject powerUpBarGroup;
    public Slider powerUpBar;

    [Header("Popups")]
    public GameObject scorePopupPrefab;
    public Canvas hudCanvas;

    private int currentScore = 0;
    private int highScore = 0;
    private float powerUpMaxDuration = 8f;
    private float powerUpRemaining = 0f;
    private bool powerUpActive = false;
    private List<GameObject> heartIcons = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnEnable()
    {
        PacmanController.OnScoreChanged += UpdateScore;
        PacmanController.OnLivesChanged += UpdateLives;
        PacmanController.OnPowerUpStart += OnPowerUpStart;
        PacmanController.OnPowerUpEnd += OnPowerUpEnd;
    }

    private void OnDisable()
    {
        PacmanController.OnScoreChanged -= UpdateScore;
        PacmanController.OnLivesChanged -= UpdateLives;
        PacmanController.OnPowerUpStart -= OnPowerUpStart;
        PacmanController.OnPowerUpEnd -= OnPowerUpEnd;
    }

    private void Start()
    {
        powerUpBarGroup?.SetActive(false);
        RefreshHighScoreText();
    }

    private void Update()
    {
        if (!powerUpActive) return;

        powerUpRemaining -= Time.deltaTime;
        powerUpBar.value = Mathf.Clamp01(powerUpRemaining / powerUpMaxDuration);

        // Flash red when almost expired
        if (powerUpRemaining <= 2f)
        {
            powerUpBar.fillRect.GetComponent<Image>().color =
                Mathf.Sin(Time.time * 10f) > 0 ? Color.red : Color.white;
        }
    }

    // ---- Public Init --------------------------------------------------------

    /// <summary>Call from GameManager when a new game/level starts.</summary>
    public void InitHUD(int lives, int level, float timerDuration, bool fruitUnlocked)
    {
        BuildLivesDisplay(lives);
        SetLevel(level);
        SetTimerDisplay(timerDuration);
        fruitCurrencyText.transform.parent.gameObject.SetActive(fruitUnlocked);
        UpdateFruitCurrency(0);
    }

    // ---- Score --------------------------------------------------------------

    private void UpdateScore(int newScore)
    {
        currentScore = newScore;
        scoreText.text = newScore.ToString("D6"); // e.g. 000420

        if (newScore > highScore)
        {
            highScore = newScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            RefreshHighScoreText();
        }
    }

    private void RefreshHighScoreText()
    {
        if (highScoreText != null)
            highScoreText.text = highScore.ToString("D6");
    }

    /// <summary>Spawns a floating +N popup at a world position.</summary>
    public void ShowScorePopup(int amount, Vector3 worldPosition)
    {
        if (scorePopupPrefab == null || hudCanvas == null) return;

        GameObject popup = Instantiate(scorePopupPrefab, hudCanvas.transform);
        TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();
        popupText.text = "+" + amount;

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudCanvas.GetComponent<RectTransform>(),
            screenPos, hudCanvas.worldCamera,
            out Vector2 canvasPos);

        popup.GetComponent<RectTransform>().localPosition = canvasPos;
        StartCoroutine(AnimatePopup(popup));
    }

    private IEnumerator AnimatePopup(GameObject popup)
    {
        RectTransform rt = popup.GetComponent<RectTransform>();
        TextMeshProUGUI text = popup.GetComponent<TextMeshProUGUI>();
        float elapsed = 0f;
        Vector2 startPos = rt.localPosition;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            rt.localPosition = startPos + Vector2.up * (50f * elapsed);
            text.alpha = 1f - elapsed;
            yield return null;
        }

        Destroy(popup);
    }

    // ---- Lives --------------------------------------------------------------

    private void UpdateLives(int lives)
    {
        BuildLivesDisplay(lives);
    }

    private void BuildLivesDisplay(int lives)
    {
        if (livesContainer == null || heartPrefab == null) return;
        foreach (var heart in heartIcons) Destroy(heart);
        heartIcons.Clear();

        for (int i = 0; i < lives; i++)
            heartIcons.Add(Instantiate(heartPrefab, livesContainer));
    }

    // ---- Level & Timer ------------------------------------------------------

    public void SetLevel(int level)
    {
        if (levelText != null) levelText.text = "LVL " + level;
    }

    /// <summary>Call every frame or second from GameManager's timer coroutine.</summary>
    public void SetTimerDisplay(float secondsRemaining)
    {
        if (timerText == null) return;
        int secs = Mathf.CeilToInt(secondsRemaining);
        timerText.text = secs.ToString("D2");
        timerText.color = secs <= 5 ? Color.red : Color.white;
    }

    // ---- Fruit Currency -----------------------------------------------------

    public void UpdateFruitCurrency(int amount)
    {
        if (fruitCurrencyText != null) fruitCurrencyText.text = "x" + amount;
    }

    // ---- Power-Up Bar -------------------------------------------------------

    private void OnPowerUpStart()
    {
        powerUpActive = true;
        powerUpRemaining = powerUpMaxDuration;
        powerUpBarGroup?.SetActive(true);
        powerUpBar.value = 1f;
        powerUpBar.fillRect.GetComponent<Image>().color = Color.cyan;
    }

    private void OnPowerUpEnd()
    {
        powerUpActive = false;
        powerUpBarGroup?.SetActive(false);
    }

    /// <summary>Call this when the player upgrades power-up duration.</summary>
    public void SetPowerUpMaxDuration(float duration) => powerUpMaxDuration = duration;
}
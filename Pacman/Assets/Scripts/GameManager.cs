using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    [Header("Testing")]
    public int testStartLevel = 1;
    public int testStartPoints = 0;
    public int testStartFruit = 0;

    [Header("Game Settings")]
    public float startingTimerDuration = 10f;
    public int startingLives = 3;

    [Header("Scene Names")]
    public string gameSceneName = "Level 0";
    public string upgradeSceneName = "UpgradeScene";

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentTimerDuration { get; private set; }

    private float timerRemaining;
    private bool timerRunning = false;
    private int pelletsRemaining = 0;
    private bool fruitUnlocked = false;

    private void Awake()
    {
        CurrentLevel = testStartLevel;
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentLevel = testStartLevel;

        // Add test currencies after a frame so PlayerUpgrades has time to initialize
        if (testStartPoints > 0 || testStartFruit > 0)
            StartCoroutine(AddTestCurrencies());
    }

    private void OnEnable() { PacmanController.OnPlayerDied += HandlePlayerDied; }
    private void OnDisable() { PacmanController.OnPlayerDied -= HandlePlayerDied; }

    public void StartGame()
    {
        CurrentLevel = 1;
        CurrentTimerDuration = startingTimerDuration;
        SceneManager.LoadScene(gameSceneName);
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameSceneName) return;
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        InitLevel();
    }

    private void InitLevel()
    {
        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        PacmanController pacman = FindObjectOfType<PacmanController>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;
        HUDManager.Instance?.InitHUD(lives, CurrentLevel, CurrentTimerDuration, fruitUnlocked);
        StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer()
    {
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;

        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            HUDManager.Instance?.SetTimerDisplay(timerRemaining);
            yield return null;
        }

        if (timerRunning) { timerRunning = false; GoToUpgradeScreen(); }
    }

    public void StopTimer() => timerRunning = false;

    // Call this from PacmanController each time a pellet is eaten
    public void OnPelletEaten()
    {
        pelletsRemaining--;
        if (pelletsRemaining <= 0) { StopTimer(); CurrentLevel++; GoToUpgradeScreen(); }
    }

    private void HandlePlayerDied() { StopTimer(); GoToUpgradeScreen(); }

    private void GoToUpgradeScreen() => SceneManager.LoadScene(upgradeSceneName);

    // Upgrade hooks - call from UpgradeManager after player picks upgrades
    public void OnUpgradesApplied()
    {
        SceneManager.LoadScene(gameSceneName);
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private IEnumerator AddTestCurrencies()
    {
        // Wait for PlayerUpgrades to be ready
        yield return new WaitUntil(() => PlayerUpgrades.Instance != null);

        if (testStartPoints > 0)
            PlayerUpgrades.Instance.AddPoints(testStartPoints);

        if (testStartFruit > 0)
            PlayerUpgrades.Instance.AddFruitCurrency(testStartFruit);
    }

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
    public void ShowScorePopup(int amount, Vector3 worldPos) => HUDManager.Instance?.ShowScorePopup(amount, worldPos);
}
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
    public float startingTimerDuration = 60f; // Set this to your desired time limit
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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Make sure these match the Inspector values immediately
        CurrentLevel = testStartLevel;
        CurrentTimerDuration = startingTimerDuration;
    }

    private void OnEnable() { testMove.OnPlayerDied += HandlePlayerDied; }
    private void OnDisable() { testMove.OnPlayerDied -= HandlePlayerDied; }

    public void StartGame()
    {
        CurrentLevel = 1;
        CurrentTimerDuration = startingTimerDuration;
        SceneManager.LoadScene(gameSceneName);
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name); // Check if this matches gameSceneName
        if (scene.name != gameSceneName) return;

        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        InitLevel();
    }

    private void InitLevel()
    {
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.1f);

        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        Time.timeScale = 1f;

        ManageHUD.Instance?.InitHUD(lives, CurrentLevel, CurrentTimerDuration, fruitUnlocked);

        // REMOVE StopAllCoroutines(); <--- This was killing the script here
        StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer()
    {
        Debug.Log("Timer Coroutine Started with: " + CurrentTimerDuration);
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;

        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;

            // This is the bridge between the two scripts
            if (ManageHUD.Instance != null)
            {
                ManageHUD.Instance.SetTimerDisplay(timerRemaining);
            }

            yield return null;
        }

        if (timerRunning)
        {
            timerRunning = false;
            HandleTimeOut();
        }
    }

    public void StopTimer() => timerRunning = false;

    public void OnPelletEaten()
    {
        pelletsRemaining--;

        // If count is low, do a physical check to prevent "1 pellet left" bugs
        if (pelletsRemaining <= 1)
        {
            int actual = GameObject.FindGameObjectsWithTag("Pellet").Length +
                         GameObject.FindGameObjectsWithTag("PowerPellet").Length;

            // We count active ones only
            int activeActual = 0;
            foreach (var p in GameObject.FindGameObjectsWithTag("Pellet")) if (p.activeInHierarchy) activeActual++;
            foreach (var p in GameObject.FindGameObjectsWithTag("PowerPellet")) if (p.activeInHierarchy) activeActual++;

            if (activeActual == 0)
            {
                WinLevel();
                return;
            }
            pelletsRemaining = activeActual;
        }

        Debug.Log($"Pellet Eaten! {pelletsRemaining} left.");
    }

    private void WinLevel()
    {
        StopTimer();
        if (ManageHUD.Instance != null)
        {
            ManageHUD.Instance.ShowWinScreen();
            Time.timeScale = 0f;
        }
    }

    private void HandleTimeOut()
    {
        StopTimer();
        // Show Lose Screen via HUD
        ManageHUD.Instance?.ShowLoseScreen();
        Time.timeScale = 0f;
    }

    private void HandlePlayerDied()
    {
        StopTimer();
        // You can choose to show Lose Screen or go straight to Upgrades
        ManageHUD.Instance?.ShowLoseScreen();
    }

    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f;
        CurrentLevel++;
        GoToUpgradeScreen();
    }

    // Called by a "Retry" button on your Lose Screen
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToUpgradeScreen() => SceneManager.LoadScene(upgradeSceneName);

    private IEnumerator AddTestCurrencies()
    {
        yield return new WaitUntil(() => PlayerUpgrades.Instance != null);
        if (testStartPoints > 0) PlayerUpgrades.Instance.AddPoints(testStartPoints);
        if (testStartFruit > 0) PlayerUpgrades.Instance.AddFruitCurrency(testStartFruit);
    }

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
}
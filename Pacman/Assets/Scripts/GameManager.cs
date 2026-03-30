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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLevel = testStartLevel;
        CurrentTimerDuration = startingTimerDuration;

        if (testStartPoints > 0 || testStartFruit > 0)
            StartCoroutine(AddTestCurrencies());
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
        // Wait a small moment to ensure all spawned pellets are registered
        yield return new WaitForSeconds(0.1f);

        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        Debug.Log($"<color=yellow>Level Initialized.</color> Total pellets found: {pelletsRemaining}");

        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        // Initialize HUD
        ManageHUD.Instance?.InitHUD(lives, CurrentLevel, CurrentTimerDuration, fruitUnlocked);

        StopAllCoroutines();
        StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer()
    {
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;

        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            // ManageHUD.Instance?.SetTimerDisplay(timerRemaining); 
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

        // Log the current count
        Debug.Log($"Pellet Eaten! Logic says {pelletsRemaining} left.");

        // SAFETY CHECK: If logic thinks we are done, OR if the scene actually has 0 pellets
        if (pelletsRemaining <= 0)
        {
            // Double check the actual scene objects to be 100% sure
            int actualCount = GameObject.FindGameObjectsWithTag("Pellet").Length +
                              GameObject.FindGameObjectsWithTag("PowerPellet").Length;

            if (actualCount == 0)
            {
                Debug.Log("<color=cyan>Win condition confirmed!</color>");
                WinLevel();
            }
            else
            {
                // This fixes the count if it got out of sync
                pelletsRemaining = actualCount;
                Debug.LogWarning($"Count was out of sync! Adjusted to actual: {actualCount}");
            }
        }
    }

    private void WinLevel()
    {
        StopTimer();

        if (ManageHUD.Instance != null)
        {
            ManageHUD.Instance.ShowWinScreen();
            Time.timeScale = 0f; // Freeze game play
        }
        else
        {
            Debug.LogError("FATAL: ManageHUD.Instance is null! Make sure ManageHUD is in the scene.");
            // Fallback so the game doesn't get stuck
            ProceedToUpgrades();
        }
    }

    private void HandlePlayerDied()
    {
        StopTimer();
        GoToUpgradeScreen();
    }

    private void HandleTimeOut() => GoToUpgradeScreen();

    // IMPORTANT: This should be called by the Button on your Win Screen
    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f; // Unfreeze the game!
        CurrentLevel++;
        GoToUpgradeScreen();
    }

    public void GoToUpgradeScreen() => SceneManager.LoadScene(upgradeSceneName);

    public void OnUpgradesApplied()
    {
        SceneManager.LoadScene(gameSceneName);
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private IEnumerator AddTestCurrencies()
    {
        yield return new WaitUntil(() => PlayerUpgrades.Instance != null);
        if (testStartPoints > 0) PlayerUpgrades.Instance.AddPoints(testStartPoints);
        if (testStartFruit > 0) PlayerUpgrades.Instance.AddFruitCurrency(testStartFruit);
    }

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
}
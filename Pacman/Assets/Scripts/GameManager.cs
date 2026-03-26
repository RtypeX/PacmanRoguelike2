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
        yield return null;

        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        // Changed HUDManager to manageHUD
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
            //ManageHUD.Instance?.SetTimerDisplay(timerRemaining); // Changed name here
            yield return null;
        }

        if (timerRunning) { timerRunning = false; HandleTimeOut(); }
    }

    public void StopTimer() => timerRunning = false;

    public void OnPelletEaten()
    {
        pelletsRemaining--;
        Debug.Log("Pellet eaten! Remaining: " + pelletsRemaining); // CHECK THE CONSOLE FOR THIS

        if (pelletsRemaining <= 0)
        {
            Debug.Log("All pellets gone! Triggering WinLevel...");
            WinLevel();
        }
    }

    private void WinLevel()
    {
        StopTimer();
        ManageHUD.Instance?.ShowWinScreen(); // Changed name here
        Time.timeScale = 0f;
    }

    private void HandlePlayerDied()
    {
        StopTimer();
        GoToUpgradeScreen();
    }

    private void HandleTimeOut() => GoToUpgradeScreen();

    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f;
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
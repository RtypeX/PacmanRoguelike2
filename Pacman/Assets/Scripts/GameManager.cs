using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static GameManager EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureCoreManagers();
            return Instance;
        }

        GameManager existing = FindObjectOfType<GameManager>();
        if (existing != null)
        {
            existing.EnsureCoreManagers();
            return existing;
        }

        GameObject gameManagerObject = new GameObject("GameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.EnsureCoreManagers();
        return gameManager;
    }

    [Header("Testing")]
    public int testStartLevel = 1; // Changed default to 1 for standard indexing
    public int testStartPoints = 0;
    public int testStartFruit = 0;

    [Header("Game Settings")]
    public float startingTimerDuration = 60f;
    public int startingLives = 3;

    [Header("Power Pellet Spawning")]
    public GameObject powerPelletPrefab;
    public Transform[] bonusPelletSpawnPoints;

    [Header("Scene Settings")]
    // We use a prefix so we can load "Level 0", "Level 1", etc. dynamically
    public string levelScenePrefix = "Level ";
    public string upgradeSceneName = "UpgradeScene";

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentTimerDuration { get; private set; }

    private float timerRemaining;
    private bool timerRunning = false;
    private int pelletsRemaining = 0;
    private bool fruitUnlocked = false;
    private Coroutine timerCoroutine;
    private bool levelInitialized = false;
    private int bonusPowerPelletCount = 0;
    private float ghostFreezeDuration = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCoreManagers();

        // Check if we have a saved level progress; otherwise use testStartLevel
        CurrentLevel = PlayerPrefs.GetInt("SavedLevel", testStartLevel);
        CurrentTimerDuration = startingTimerDuration;

        if (testStartPoints > 0 || testStartFruit > 0)
            StartCoroutine(AddTestCurrencies());
    }

    private void OnEnable()
    {
        testMove.OnPlayerDied += HandlePlayerDied;
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private void OnDisable()
    {
        testMove.OnPlayerDied -= HandlePlayerDied;
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
    }

    // --- CALL THIS FOR A COMPLETELY NEW SAVE ---
    public void StartGame()
    {
        CurrencyManager.Instance?.ResetForNewRun();
        PlayerUpgrades.Instance?.ResetUpgrades();

        CurrentLevel = 1;
        PlayerPrefs.SetInt("SavedLevel", 1); // Reset save data
        LoadLevelByIndex(CurrentLevel);
        CurrentTimerDuration = startingTimerDuration;
        fruitUnlocked = false;
        bonusPowerPelletCount = 0;
        ghostFreezeDuration = 0f;

        LoadLevelByIndex(CurrentLevel);
    }

    // --- CALL THIS FROM THE MAIN MENU PLAY BUTTON ---
    public void LoadLevelFromMenu()
    {
        levelInitialized = false;
        pelletsRemaining = 999;
        timerRunning = false;
        Time.timeScale = 1f;

        LoadLevelByIndex(CurrentLevel);
    }

    private void LoadLevelByIndex(int levelNum)
    {
        // Math: Level 1 = "Level 0", Level 2 = "Level 1"
        string sceneToLoad = levelScenePrefix + (levelNum - 1);

        // CHECK: Does this scene actually exist in Build Settings?
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.Log($"Loading Scene: {sceneToLoad} (CurrentLevel: {levelNum})");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            // If it doesn't exist (e.g., you finished the game), loop or stay on last level
            Debug.LogWarning($"Scene {sceneToLoad} not found! Staying on highest level.");
            CurrentLevel = Mathf.Max(1, levelNum - 1);
            PlayerPrefs.SetInt("SavedLevel", CurrentLevel);
            SceneManager.LoadScene(levelScenePrefix + (CurrentLevel - 1));
        }
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Any scene starting with "Level " will trigger initialization
        if (!scene.name.StartsWith(levelScenePrefix)) return;
        InitLevel();
    }

    private void InitLevel()
    {
        Time.timeScale = 1f; // Ensure physics are running
        levelInitialized = false;
        StopTimer();
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForEndOfFrame();

        if (bonusPowerPelletCount > 0)
            SpawnBonusPowerPellets(bonusPowerPelletCount);

        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        Debug.Log($"Level Initialized. Pellets to eat: {pelletsRemaining}");

        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        ManageHUD.Instance?.InitHUD(lives, CurrentLevel, CurrentTimerDuration, fruitUnlocked);

        if (pacman != null)
            ManageHUD.Instance?.SetPowerUpMaxDuration(pacman.powerUpDuration);

        FruitSpawner fruitSpawner = FindObjectOfType<FruitSpawner>();
        fruitSpawner?.TrySpawnForLevelStart();

        if (ghostFreezeDuration > 0f)
            StartCoroutine(FreezeGhosts(ghostFreezeDuration));

        levelInitialized = true;
        StartTimer();
    }

    public void OnPelletEaten()
    {
        if (!levelInitialized) return;
        pelletsRemaining--;
        if (pelletsRemaining <= 0) WinLevel();
    }

    private void WinLevel()
    {
        levelInitialized = false;
        StopTimer();
        ManageHUD.Instance?.ShowWinScreen();
        Time.timeScale = 0f;
    }

    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f;

        // Peek ahead: Does the NEXT level exist?
        string nextLevelName = levelScenePrefix + CurrentLevel; // If current is 1, checks "Level 1"

        if (Application.CanStreamedLevelBeLoaded(nextLevelName))
        {
            CurrentLevel++;
            PlayerPrefs.SetInt("SavedLevel", CurrentLevel);
            Debug.Log("Progressing to Level " + (CurrentLevel - 1));
        }
        else
        {
            Debug.Log("No more levels found in Build Settings. Replaying last level.");
        }

        GoToUpgradeScreen();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        StopTimer();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToUpgradeScreen()
    {
        levelInitialized = false;
        Time.timeScale = 1f;
        StopTimer();
        SceneManager.LoadScene(upgradeSceneName);
    }

    // ... (Keep existing Ghost freezing, Timer, and Currency methods)
    private void SpawnBonusPowerPellets(int count)
    {
        if (powerPelletPrefab == null || bonusPelletSpawnPoints == null || bonusPelletSpawnPoints.Length == 0)
            return;

        List<Transform> available = new List<Transform>(bonusPelletSpawnPoints);
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = available[i];
            available[i] = available[j];
            available[j] = tmp;
        }

        int toSpawn = Mathf.Min(count, available.Count);
        for (int i = 0; i < toSpawn; i++)
            Instantiate(powerPelletPrefab, available[i].position, Quaternion.identity);
    }

    private IEnumerator FreezeGhosts(float duration)
    {
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts) ghost.SetFrozen(true);
        yield return new WaitForSeconds(duration);
        foreach (GhostController ghost in ghosts) if (ghost != null) ghost.SetFrozen(false);
    }

    private void StartTimer() { StopTimer(); timerCoroutine = StartCoroutine(RunTimer()); }

    private IEnumerator RunTimer()
    {
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;
        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            if (ManageHUD.Instance != null) ManageHUD.Instance.SetTimerDisplay(timerRemaining);
            yield return null;
        }
        if (timerRunning && timerRemaining <= 0) { timerRunning = false; HandleTimeOut(); }
    }

    public void StopTimer() { timerRunning = false; if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; } }

    private void HandleTimeOut() { if (!levelInitialized) return; StopTimer(); ManageHUD.Instance?.ShowLoseScreen(); Time.timeScale = 0f; }

    private void HandlePlayerDied() { if (!levelInitialized) return; StopTimer(); ManageHUD.Instance?.ShowLoseScreen(); Time.timeScale = 0f; }

    public void PacmanEaten() => HandlePlayerDied();

    public void GhostEaten(Ghost ghost)
    {
        if (ghost == null) return;
        CurrencyManager.Instance?.AddPoints(ghost.points);
        ghost.gameObject.SetActive(false);
    }

    private IEnumerator AddTestCurrencies()
    {
        yield return new WaitUntil(() => CurrencyManager.Instance != null && PlayerUpgrades.Instance != null);
        if (testStartPoints > 0) CurrencyManager.Instance.AddPoints(testStartPoints);
        if (testStartFruit > 0) { CurrencyManager.Instance.UnlockFruit(); CurrencyManager.Instance.AddFruitCurrency(testStartFruit); }
    }

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
    public void UpgradePowerPelletCount(int count) => bonusPowerPelletCount += count;
    public void UpgradeGhostFreeze(float duration) => ghostFreezeDuration += duration;

    private void EnsureCoreManagers()
    {
        if (CurrencyManager.Instance == null && GetComponent<CurrencyManager>() == null) gameObject.AddComponent<CurrencyManager>();
        if (PlayerUpgrades.Instance == null && GetComponent<PlayerUpgrades>() == null) gameObject.AddComponent<PlayerUpgrades>();
    }
}
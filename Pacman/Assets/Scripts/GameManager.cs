using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Testing")]
    public int testStartLevel = 2;
    public int testStartPoints = 0;
    public int testStartFruit = 0;

    [Header("Game Settings")]
    public float startingTimerDuration = 60f;
    public int startingLives = 3;

    [Header("Power Pellet Spawning")]
    public GameObject powerPelletPrefab;
    public Transform[] bonusPelletSpawnPoints;

    [Header("Scene Names")]
    public string gameSceneName = "Level 0";
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

        CurrentLevel = testStartLevel;
        CurrentTimerDuration = startingTimerDuration;

        if (testStartPoints > 0 || testStartFruit > 0)
            StartCoroutine(AddTestCurrencies());
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == gameSceneName)
            InitLevel();
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
        CurrentTimerDuration = startingTimerDuration;
        fruitUnlocked = false;
        bonusPowerPelletCount = 0;
        ghostFreezeDuration = 0f;

        SceneManager.LoadScene(gameSceneName);
    }

    // --- CALL THIS FROM THE MAIN MENU IF THEY JUST CAME FROM THE SHOP ---
    public void LoadLevelFromMenu()
    {
        // Reset state so old level data doesn't carry over
        levelInitialized = false;
        pelletsRemaining = 999; // Set to a high number so check doesn't trigger 0 instantly
        timerRunning = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameSceneName) return;
        InitLevel();
    }

    private void InitLevel()
    {
        levelInitialized = false;
        StopTimer();
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        // Give the scene a moment to instantiate all objects
        yield return new WaitForEndOfFrame();

        // 1. Spawn any bonus items from upgrades
        if (bonusPowerPelletCount > 0)
            SpawnBonusPowerPellets(bonusPowerPelletCount);

        // 2. COUNT PELLETS FIRST - Before we do anything else
        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        Debug.Log($"Level Initialized. Pellets to eat: {pelletsRemaining}");

        // 3. Find Player and HUD
        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        Time.timeScale = 1f;

        ManageHUD.Instance?.InitHUD(lives, CurrentLevel, CurrentTimerDuration, fruitUnlocked);

        if (pacman != null)
            ManageHUD.Instance?.SetPowerUpMaxDuration(pacman.powerUpDuration);

        if (ghostFreezeDuration > 0f)
            StartCoroutine(FreezeGhosts(ghostFreezeDuration));

        // 4. Start the game logic
        levelInitialized = true;
        StartTimer();
    }

    public void OnPelletEaten()
    {
        // Don't check for win if the level is still setting up
        if (!levelInitialized) return;

        pelletsRemaining--;

        if (pelletsRemaining <= 0)
        {
            WinLevel();
        }
    }

    private void WinLevel()
    {
        levelInitialized = false; // Prevent double triggers
        StopTimer();
        ManageHUD.Instance?.ShowWinScreen();
        Time.timeScale = 0f;
    }

    // ... (Keep your SpawnBonusPowerPellets, FreezeGhosts, and Timer methods exactly as they were)

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
        foreach (GhostController ghost in ghosts)
            ghost.SetFrozen(true);

        yield return new WaitForSeconds(duration);

        foreach (GhostController ghost in ghosts)
            if (ghost != null) ghost.SetFrozen(false);
    }

    private void StartTimer()
    {
        StopTimer();
        timerCoroutine = StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer()
    {
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;

        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            if (ManageHUD.Instance != null)
                ManageHUD.Instance.SetTimerDisplay(timerRemaining);

            yield return null;
        }

        if (timerRunning && timerRemaining <= 0)
        {
            timerRunning = false;
            HandleTimeOut();
        }
    }

    public void StopTimer()
    {
        timerRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private void HandleTimeOut()
    {
        if (!levelInitialized) return;
        StopTimer();
        //animator.SetTrigger("Death");
        ManageHUD.Instance?.ShowLoseScreen();
        Time.timeScale = 0f;
    }

    private void HandlePlayerDied()
    {
        if (!levelInitialized) return;
        StopTimer();
        ManageHUD.Instance?.ShowLoseScreen();
        Time.timeScale = 0f;
    }

    public void PacmanEaten()
    {
        HandlePlayerDied();
    }

    public void GhostEaten(Ghost ghost)
    {
        if (ghost == null)
        {
            return;
        }

        CurrencyManager.Instance?.AddPoints(ghost.points);
        ghost.gameObject.SetActive(false);
    }

    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f;
        CurrentLevel++;
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

    private IEnumerator AddTestCurrencies()
    {
        yield return new WaitUntil(() =>
            CurrencyManager.Instance != null && PlayerUpgrades.Instance != null);

        if (testStartPoints > 0)
            CurrencyManager.Instance.AddPoints(testStartPoints);

        if (testStartFruit > 0)
        {
            CurrencyManager.Instance.UnlockFruit();
            CurrencyManager.Instance.AddFruitCurrency(testStartFruit);
        }
    }

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
    public void UpgradePowerPelletCount(int count) => bonusPowerPelletCount += count;
    public void UpgradeGhostFreeze(float duration) => ghostFreezeDuration += duration;
}

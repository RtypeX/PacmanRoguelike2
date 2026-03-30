using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Power Pellet Spawning")]
    [Tooltip("Prefab used when spawning bonus power pellets. Must have the 'PowerPellet' tag.")]
    public GameObject powerPelletPrefab;

    [Tooltip("All possible spawn positions for bonus power pellets in the maze.")]
    public Transform[] bonusPelletSpawnPoints;

    [Header("Scene Names")]
    public string gameSceneName = "Level 0";
    public string upgradeSceneName = "UpgradeScene";

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentTimerDuration { get; private set; }

    private float timerRemaining = 0f;
    private bool timerRunning = false;
    private int pelletsRemaining = 0;
    private bool fruitUnlocked = false;
    private Coroutine timerCoroutine;

    // Bonus power pellets accumulated from upgrades this run
    private int bonusPowerPelletCount = 0;

    // Ghost freeze duration accumulated from upgrades this run
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

    private void OnEnable() { testMove.OnPlayerDied += HandlePlayerDied; }
    private void OnDisable() { testMove.OnPlayerDied -= HandlePlayerDied; }

    // ---- Scene Flow ---------------------------------------------------------

    public void StartGame()
    {
        CurrencyManager.Instance?.ResetForNewRun();
        PlayerUpgrades.Instance?.ResetUpgrades();

        CurrentLevel = 1;
        CurrentTimerDuration = startingTimerDuration;
        fruitUnlocked = false;
        bonusPowerPelletCount = 0;
        ghostFreezeDuration = 0f;
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
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

        // Spawn any bonus power pellets earned through upgrades
        if (bonusPowerPelletCount > 0)
            SpawnBonusPowerPellets(bonusPowerPelletCount);

        // Count ALL pellets (including newly spawned bonus ones)
        pelletsRemaining = GameObject.FindGameObjectsWithTag("Pellet").Length
                         + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        Debug.Log($"<color=yellow>Level Initialized.</color> Total pellets found: {pelletsRemaining}");

        testMove pacman = FindObjectOfType<testMove>();
        int lives = pacman != null ? pacman.CurrentLives : startingLives;

        // Initialize HUD
        HUDManager.Instance?.InitHUD(
            lives,
            CurrentLevel,
            CurrentTimerDuration,
            fruitUnlocked,
            CurrencyManager.Instance != null ? CurrencyManager.Instance.FruitCurrency : 0);
        HUDManager.Instance?.SetPowerUpMaxDuration(pacman != null ? pacman.powerUpDuration : 8f);

        // Apply ghost freeze before starting the timer
        if (ghostFreezeDuration > 0f)
            StartCoroutine(FreezeGhosts(ghostFreezeDuration));

        StartTimer();
    }

    // ---- Power Pellet Spawning ----------------------------------------------

    /// <summary>
    /// Picks random positions from bonusPelletSpawnPoints and instantiates
    /// powerPelletPrefab up to <count> times. Skips gracefully if prefab or
    /// spawn points are not assigned.
    /// </summary>
    private void SpawnBonusPowerPellets(int count)
    {
        if (powerPelletPrefab == null)
        {
            Debug.LogWarning("GameManager: powerPelletPrefab is not assigned. Cannot spawn bonus power pellets.");
            return;
        }

        if (bonusPelletSpawnPoints == null || bonusPelletSpawnPoints.Length == 0)
        {
            Debug.LogWarning("GameManager: bonusPelletSpawnPoints is empty. Cannot spawn bonus power pellets.");
            return;
        }

        // Shuffle a copy of the spawn point array so we don't repeat positions
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
        {
            Instantiate(powerPelletPrefab, available[i].position, Quaternion.identity);
            Debug.Log($"<color=cyan>Spawned bonus power pellet</color> at {available[i].position}");
        }

        if (count > available.Count)
            Debug.LogWarning($"GameManager: Tried to spawn {count} bonus power pellets but only {available.Count} spawn points exist.");
    }

    // ---- Ghost Freeze -------------------------------------------------------

    /// <summary>
    /// Freezes active ghosts for <duration> seconds, then unfreezes them.
    /// </summary>
    private IEnumerator FreezeGhosts(float duration)
    {
        GhostController[] ghosts = FindObjectsOfType<GhostController>();

        if (ghosts.Length == 0)
        {
            Debug.LogWarning("GameManager: GhostFreeze upgrade active but no GhostController components found in scene.");
            yield break;
        }

        foreach (GhostController ghost in ghosts)
            ghost.SetFrozen(true);

        Debug.Log($"<color=magenta>Ghosts frozen</color> for {duration} seconds.");

        yield return new WaitForSeconds(duration);

        foreach (GhostController ghost in ghosts)
            if (ghost != null) ghost.SetFrozen(false);

        Debug.Log("<color=magenta>Ghosts unfrozen.</color>");
    }

    // ---- Timer --------------------------------------------------------------

    private void StartTimer()
    {
        StopTimer();
        timerCoroutine = StartCoroutine(RunTimer());
    }

    private IEnumerator RunTimer()
    {
        timerRunning = true;
        timerRemaining = CurrentTimerDuration;
        HUDManager.Instance?.SetTimerDisplay(timerRemaining);

        while (timerRemaining > 0f && timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            HUDManager.Instance?.SetTimerDisplay(timerRemaining);
            yield return null;
        }

        if (timerRunning)
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

    // ---- Pellet / Win -------------------------------------------------------

    public void OnPelletEaten()
    {
        pelletsRemaining--;
        Debug.Log($"<color=green>Pellet Eaten!</color> {pelletsRemaining} left.");

        if (pelletsRemaining <= 0)
        {
            Debug.Log("<color=cyan>Win condition met!</color> Triggering Win Screen.");
            WinLevel();
        }
    }

    private void WinLevel()
    {
        StopTimer();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowWinScreen();
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogError("FATAL: HUDManager.Instance is null! Make sure HUDManager is in the scene.");
            ProceedToUpgrades();
        }
    }

    // ---- Death / Timeout ----------------------------------------------------

    private void HandlePlayerDied() { StopTimer(); GoToUpgradeScreen(); }
    private void HandleTimeOut() => GoToUpgradeScreen();

    // ---- Scene Transitions --------------------------------------------------

    // IMPORTANT: Called by the Button on your Win Screen
    public void ProceedToUpgrades()
    {
        Time.timeScale = 1f;
        CurrentLevel++;
        GoToUpgradeScreen();
    }

    public void GoToUpgradeScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(upgradeSceneName);
    }

    public void OnUpgradesApplied()
    {
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    // ---- Upgrade Callbacks --------------------------------------------------

    public void UpgradeTimerDuration(float bonus)
    {
        CurrentTimerDuration += bonus;
    }

    public void UnlockFruit()
    {
        fruitUnlocked = true;
    }

    /// <summary>
    /// Called by PlayerUpgrades when ExtraPowerPellets upgrade is purchased.
    /// Accumulates total so every future level spawns the right number.
    /// </summary>
    public void UpgradePowerPelletCount(int count)
    {
        bonusPowerPelletCount += count;
        Debug.Log($"<color=cyan>Power pellet upgrade applied.</color> Total bonus pellets per level: {bonusPowerPelletCount}");
    }

    /// <summary>
    /// Called by PlayerUpgrades when GhostFreeze upgrade is purchased.
    /// Accumulates total freeze duration for future levels.
    /// </summary>
    public void UpgradeGhostFreeze(float duration)
    {
        ghostFreezeDuration += duration;
        Debug.Log($"<color=magenta>Ghost freeze upgrade applied.</color> Total freeze duration: {ghostFreezeDuration}s");
    }

    // ---- Test Currencies ----------------------------------------------------

    private IEnumerator AddTestCurrencies()
    {
        // Wait for both managers to be alive
        yield return new WaitUntil(() =>
            CurrencyManager.Instance != null && PlayerUpgrades.Instance != null);

        if (testStartPoints > 0)
            CurrencyManager.Instance.AddPoints(testStartPoints);

        // Fruit currency requires FruitUnlocked — force-unlock for testing
        if (testStartFruit > 0)
        {
            CurrencyManager.Instance.UnlockFruit();
            CurrencyManager.Instance.AddFruitCurrency(testStartFruit);
        }
    }
}

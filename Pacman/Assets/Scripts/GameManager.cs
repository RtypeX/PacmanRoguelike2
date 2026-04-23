using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private static bool sessionStateInitialized = false;
    private static int sessionCurrentLevel = 1;
    private static bool sessionHasCompletedLevel = false;

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
    public int testStartLevel = 0;
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
    public bool HasCompletedLevel { get; private set; } = false;

    // runtime state
    private float timerRemaining;
    private bool timerRunning = false;
    private int pelletsRemaining = 0;
    private bool fruitUnlocked = false;
    private Coroutine timerCoroutine;
    private bool levelInitialized = false;
    private int bonusPowerPelletCount = 0;
    private float ghostFreezeDuration = 0f;

    // --- Added/merged members from provided script (non-destructive) ---
    [Header("Optional HUD / Classic Game fields (set in Inspector)")]
    [SerializeField] private Ghost[] ghosts;                 // optional: assign your Ghost objects
    [SerializeField] private Transform pellets;              // optional: parent transform containing pellet children
    [SerializeField] private Text gameOverText;              // optional: UI text for classic game over message
    [SerializeField] private Text scoreText;                 // optional: UI text to show score
    [SerializeField] private Text livesText;                 // optional: UI text to show lives

    // Score / lives state (kept separate from other systems)
    public int score { get; private set; } = 0;
    public int lives { get; private set; } = 3;
    private int ghostMultiplier = 1;
    // ----------------------------------------------------------------

    // cache for player reference used in many places
    private testMove pacman;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCoreManagers();

        CurrentTimerDuration = startingTimerDuration;

        if (sessionStateInitialized)
        {
            CurrentLevel = sessionCurrentLevel;
            HasCompletedLevel = sessionHasCompletedLevel;
            Debug.Log($"GameManager.Awake restored session state CurrentLevel={CurrentLevel}, HasCompletedLevel={HasCompletedLevel}.");
        }
        else
        {
            CurrentLevel = Mathf.Max(1, testStartLevel + 1);
            HasCompletedLevel = testStartLevel > 0;
            SyncSessionState();
            Debug.Log($"GameManager.Awake initialized CurrentLevel={CurrentLevel}, HasCompletedLevel={HasCompletedLevel}, testStartLevel={testStartLevel}.");
        }

        if (testStartPoints > 0 || testStartFruit > 0)
            StartCoroutine(AddTestCurrencies());
    }

    private void Start()
    {
        if (IsGameplayScene(SceneManager.GetActiveScene().name))
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
        HasCompletedLevel = false;
        fruitUnlocked = false;
        bonusPowerPelletCount = 0;
        ghostFreezeDuration = 0f;
        SyncSessionState();

        // reset classic-game-like fields
        score = 0;
        ghostMultiplier = 1;
        lives = startingLives;

        string sceneToLoad = GetSceneNameForLevel(CurrentLevel);
        Debug.Log($"GameManager.StartGame starting new run at CurrentLevel={CurrentLevel}, loading '{sceneToLoad}'.");
        SceneManager.LoadScene(sceneToLoad);
    }

    // --- CALL THIS FROM THE MAIN MENU IF THEY JUST CAME FROM THE SHOP ---
    public void LoadLevelFromMenu()
    {
        // Reset state so old level data doesn't carry over
        levelInitialized = false;
        pelletsRemaining = 999; // Set to a high number so check doesn't trigger 0 instantly
        timerRunning = false;

        Time.timeScale = 1f;
        string sceneToLoad = GetSceneNameForLevel(CurrentLevel);
        Debug.Log($"GameManager.LoadLevelFromMenu resuming with CurrentLevel={CurrentLevel}, HasCompletedLevel={HasCompletedLevel}, loading '{sceneToLoad}'.");
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene.name)) return;
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
        pacman = FindObjectOfType<testMove>();
        int livesLocal = pacman != null ? pacman.CurrentLives : startingLives;

        Time.timeScale = 1f;

        ManageHUD.Instance?.InitHUD(livesLocal, CurrentLevel, CurrentTimerDuration, fruitUnlocked);

        if (pacman != null)
            ManageHUD.Instance?.SetPowerUpMaxDuration(pacman.powerUpDuration);

        FruitSpawner fruitSpawner = FindObjectOfType<FruitSpawner>();
        Debug.Log($"GameManager.DelayedInit fruitSpawner found={fruitSpawner != null}, fruitUnlocked={fruitUnlocked}, currencyFruitUnlocked={(CurrencyManager.Instance != null && CurrencyManager.Instance.FruitUnlocked)}");
        fruitSpawner?.TrySpawnForLevelStart();

        if (ghostFreezeDuration > 0f)
            StartCoroutine(FreezeGhosts(ghostFreezeDuration));

        // If using classic-style UI elements, update them now
        UpdateHUDScoreAndLives();

        // 4. Start the game logic
        levelInitialized = true;
        StartTimer();
    }

    public void OnPelletEaten()
    {
        // Don't check for win if the level is still setting up
        if (!levelInitialized)
        {
            Debug.Log("Player died. LevelInitialized: " + levelInitialized);
            return;
        }

        pelletsRemaining--;

        if (pelletsRemaining <= 0)
        {
            WinLevel();
        }
    }

    private void WinLevel()
    {
        levelInitialized = false; // Prevent double triggers
        HasCompletedLevel = true;
        SyncSessionState();
        Debug.Log($"GameManager.WinLevel marked HasCompletedLevel={HasCompletedLevel} at CurrentLevel={CurrentLevel}.");
        StopTimer();
        ManageHUD.Instance?.ShowWinScreen();
        Time.timeScale = 0f;
    }

    // Spawn/Freeze/Timer logic kept as before
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
        Ghost[] ghostsFound = FindObjectsOfType<Ghost>();
        foreach (Ghost ghost in ghostsFound)
            ghost.SetFrozen(true);

        yield return new WaitForSeconds(duration);

        foreach (Ghost ghost in ghostsFound)
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
        HasCompletedLevel = true;
        SyncSessionState();
        Debug.Log($"GameManager.ProceedToUpgrades incremented CurrentLevel to {CurrentLevel} before loading '{upgradeSceneName}'.");
        GoToUpgradeScreen();
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        StopTimer();

        string activeSceneName = SceneManager.GetActiveScene().name;
        int currentSceneNumber = GetSceneNumberFromName(activeSceneName);
        int nextSceneNumber = currentSceneNumber >= 0 ? currentSceneNumber + 1 : Mathf.Max(1, CurrentLevel);
        string nextSceneName = $"Level {nextSceneNumber}";

        if (SceneExistsInBuildSettings(nextSceneName))
        {
            CurrentLevel = nextSceneNumber + 1;
            HasCompletedLevel = true;
            SyncSessionState();
            Debug.Log($"GameManager.LoadNextLevel advancing from '{activeSceneName}' to '{nextSceneName}' with CurrentLevel={CurrentLevel}.");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        Debug.LogWarning($"GameManager.LoadNextLevel could not find '{nextSceneName}'. Staying on current progression state and opening upgrades instead.");
        GoToUpgradeScreen();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        StopTimer();
        SceneManager.LoadScene(GetSceneNameForLevel(CurrentLevel));
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

    // ---------------------------
    // Methods merged/added from provided script — non-destructive additions
    // ---------------------------

    // Classic-style: set the score and update optional UI text
    private void SetScore(int newScore)
    {
        score = newScore;
        // Update classic UI if assigned
        if (scoreText != null)
            scoreText.text = score.ToString().PadLeft(2, '0');

        // Also inform ManageHUD if present
        ManageHUD.Instance?.SendMessage("SetScore", score, SendMessageOptions.DontRequireReceiver);
    }

    // Classic-style: set the lives and update optional UI text
    private void SetLives(int newLives)
    {
        lives = newLives;
        if (livesText != null)
            livesText.text = "x" + lives.ToString();

        ManageHUD.Instance?.SendMessage("SetLives", lives, SendMessageOptions.DontRequireReceiver);
    }

    // Classic-style: reset state for ghosts and pacman (non-destructive; uses existing Reset flows)
    private void ResetState()
    {
        // Reset Ghosts if a Ghost[] was assigned (provided script pattern)
        if (ghosts != null && ghosts.Length > 0)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] != null)
                    ghosts[i].ResetState();
            }
        }
        else
        {
            // fallback: find all Ghost components and reset them
            Ghost[] allGhosts = FindObjectsOfType<Ghost>();
            foreach (var g in allGhosts)
                g.ResetState();
        }

        // Reset pacman: prefer an inspector-assigned Pacman/testMove, otherwise find one
        if (pacman == null)
            pacman = FindObjectOfType<testMove>();

        pacman?.SendMessage("ResetState", SendMessageOptions.DontRequireReceiver);
    }

    // Classic-style: game over behavior (non-destructive)
    private void GameOver()
    {
        // Mirror existing lose flow
        StopTimer();
        ManageHUD.Instance?.ShowLoseScreen();

        // Show classical gameOverText if assigned
        if (gameOverText != null)
            gameOverText.enabled = true;

        Time.timeScale = 0f;
    }

    // Classic-style: a pellet was eaten with a Pellet object (optional)
    // This does not replace existing OnPelletEaten(), it complements it.
    public void PelletEaten(Pellet pellet)
    {
        if (pellet == null) return;

        pellet.gameObject.SetActive(false);
        SetScore(score + pellet.points);

        // If a pellets container Transform is assigned, check for remaining pellets
        if (pellets != null)
        {
            if (!HasRemainingPellets())
            {
                // mimic provided script behavior
                if (pacman == null) pacman = FindObjectOfType<testMove>();
                pacman?.gameObject.SetActive(false);
                Invoke(nameof(NewRound), 3f);
            }
        }
    }

    // Classic-style: power pellet consumed (optional)
    public void PowerPelletEaten(PowerPellet pellet)
    {
        if (pellet == null) return;

        // enable frightened behavior on assigned Ghost[] if present
        if (ghosts != null && ghosts.Length > 0)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] != null && ghosts[i].frightened != null)
                    ghosts[i].frightened.Enable(pellet.duration);
            }
        }

        PelletEaten(pellet);
        CancelInvoke(nameof(ResetGhostMultiplier));
        Invoke(nameof(ResetGhostMultiplier), pellet.duration);
    }

    private bool HasRemainingPellets()
    {
        if (pellets == null) return true;
        foreach (Transform pellet in pellets)
        {
            if (pellet.gameObject.activeSelf) return true;
        }
        return false;
    }

    private void ResetGhostMultiplier()
    {
        ghostMultiplier = 1;
    }

    private void NewRound()
    {
        if (gameOverText != null) gameOverText.enabled = false;

        if (pellets != null)
        {
            foreach (Transform pellet in pellets)
                pellet.gameObject.SetActive(true);
        }

        ResetState();
    }

    // Helper to update classic UI and ManageHUD
    private void UpdateHUDScoreAndLives()
    {
        if (scoreText != null)
            scoreText.text = score.ToString().PadLeft(2, '0');

        if (livesText != null)
            livesText.text = "x" + lives.ToString();

        ManageHUD.Instance?.SendMessage("SetScore", score, SendMessageOptions.DontRequireReceiver);
        ManageHUD.Instance?.SendMessage("SetLives", lives, SendMessageOptions.DontRequireReceiver);
    }

    // ---------------------------

    public void UpgradeTimerDuration(float bonus) => CurrentTimerDuration += bonus;
    public void UnlockFruit() => fruitUnlocked = true;
    public void UpgradePowerPelletCount(int count) => bonusPowerPelletCount += count;
    public void UpgradeGhostFreeze(float duration) => ghostFreezeDuration += duration;
    public void DebugMarkLevelCompleted()
    {
        HasCompletedLevel = true;
        SyncSessionState();
        Debug.Log($"GameManager.DebugMarkLevelCompleted set HasCompletedLevel={HasCompletedLevel} at CurrentLevel={CurrentLevel}.");
    }

    public void DebugSetCurrentLevel(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
        SyncSessionState();
        Debug.Log($"GameManager.DebugSetCurrentLevel set CurrentLevel={CurrentLevel}.");
    }

    private void EnsureCoreManagers()
    {
        if (CurrencyManager.Instance == null && GetComponent<CurrencyManager>() == null)
        {
            gameObject.AddComponent<CurrencyManager>();
            Debug.Log("GameManager.EnsureCoreManagers created CurrencyManager.");
        }

        if (PlayerUpgrades.Instance == null && GetComponent<PlayerUpgrades>() == null)
        {
            gameObject.AddComponent<PlayerUpgrades>();
            Debug.Log("GameManager.EnsureCoreManagers created PlayerUpgrades.");
        }
    }

    private bool IsGameplayScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               (sceneName == gameSceneName || sceneName.StartsWith("Level "));
    }

    private string GetSceneNameForLevel(int level)
    {
        int sceneNumber = Mathf.Max(0, level - 1);
        string numberedSceneName = $"Level {sceneNumber}";

        if (SceneExistsInBuildSettings(numberedSceneName))
            return numberedSceneName;

        return gameSceneName;
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrWhiteSpace(scenePath))
                continue;

            string buildSceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (buildSceneName == sceneName)
                return true;
        }

        return false;
    }

    private int GetSceneNumberFromName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !sceneName.StartsWith("Level "))
            return -1;

        string suffix = sceneName.Substring("Level ".Length);
        return int.TryParse(suffix, out int sceneNumber) ? sceneNumber : -1;
    }

    private void SyncSessionState()
    {
        sessionStateInitialized = true;
        sessionCurrentLevel = CurrentLevel;
        sessionHasCompletedLevel = HasCompletedLevel;
    }
}


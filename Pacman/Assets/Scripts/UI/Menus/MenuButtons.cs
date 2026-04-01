using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MenuButtons : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button upgradesButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Lock Notification")]
    public TextMeshProUGUI playFirstNotification;

    private GameManager cachedGameManager;

    private void Start()
    {

        if (startButton == null) startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (upgradesButton == null) upgradesButton = GameObject.Find("UpgradesButton")?.GetComponent<Button>();
        if (settingsButton == null) settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        if (quitButton == null) quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();

        playFirstNotification?.gameObject.SetActive(false);

        cachedGameManager = GetOrCreateGameManager();

        startButton?.onClick.AddListener(() =>
        {
            cachedGameManager = GetOrCreateGameManager();

            if (cachedGameManager != null)
            {
                // LOGIC CHANGE: 
                // If the player is on Level 1, it's a "New Game" (Reset).
                // If CurrentLevel is 2 or higher, they are returning from the shop (Don't Reset).
                if (cachedGameManager.CurrentLevel == 1)
                {
                    cachedGameManager.StartGame();
                }
                else
                {
                    cachedGameManager.LoadLevelFromMenu();
                }
            }
            else
            {
                SceneManager.LoadScene("Level 0");
            }
        });
        settingsButton?.onClick.AddListener(() => SceneManager.LoadScene("SettingsScene"));
        quitButton?.onClick.AddListener(() => Application.Quit());

        upgradesButton?.onClick.AddListener(TryOpenUpgrades);

        // Grey out upgrades button if no level completed
        RefreshUpgradeButton();
    }

    private void TryOpenUpgrades()
    {
        cachedGameManager = GetOrCreateGameManager();
        bool hasPlayedLevel = cachedGameManager == null || cachedGameManager.CurrentLevel > 1;

        if (!hasPlayedLevel)
        {
            StartCoroutine(ShowNotification());
            return;
        }

        SceneManager.LoadScene("UpgradeScene");
    }

    private void RefreshUpgradeButton()
    {
        if (upgradesButton == null) return;

        cachedGameManager = GetOrCreateGameManager();
        bool hasPlayedLevel = cachedGameManager == null || cachedGameManager.CurrentLevel > 1;

        // Grey out the button visually if locked
        ColorBlock colors = upgradesButton.colors;
        colors.normalColor = hasPlayedLevel ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.highlightedColor = hasPlayedLevel ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
        upgradesButton.colors = colors;
    }

    private IEnumerator ShowNotification()
    {
        if (playFirstNotification == null) yield break;

        playFirstNotification.gameObject.SetActive(true);

        Color c = playFirstNotification.color;

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
            playFirstNotification.color = c;
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(1.5f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            playFirstNotification.color = c;
            yield return null;
        }

        playFirstNotification.gameObject.SetActive(false);
    }

    private GameManager GetOrCreateGameManager()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance;

        GameManager existing = FindObjectOfType<GameManager>();
        if (existing != null)
            return existing;

        GameObject gameManagerObject = new GameObject("GameManager");
        return gameManagerObject.AddComponent<GameManager>();
    }
}

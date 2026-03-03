using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtons : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button upgradesButton;
    public Button settingsButton;

    [Header("In-Game / Other Buttons")]
    public Button quitButton;
    public Button backToMenuButton;

    private void Start()
    {
        startButton?.onClick.AddListener(() => GameManager.Instance.StartGame());
        upgradesButton?.onClick.AddListener(() => SceneManager.LoadScene("UpgradeScene"));
        settingsButton?.onClick.AddListener(() => SceneManager.LoadScene("SettingsScene"));
        backToMenuButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        quitButton?.onClick.AddListener(() => Application.Quit());
    }
}
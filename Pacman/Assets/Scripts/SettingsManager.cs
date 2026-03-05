using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Navigation")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public TextMeshProUGUI pageIndicatorText;
    public TextMeshProUGUI cardTitleText;
    public RectTransform cardPanel;

    [Header("Card Panels")]
    public GameObject audioPanel;
    public GameObject displayPanel;
    public GameObject keybindingsPanel;
    public GameObject resetPanel;

    [Header("Audio")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Keybindings")]
    public TextMeshProUGUI moveUpText;
    public TextMeshProUGUI moveDownText;
    public TextMeshProUGUI moveLeftText;
    public TextMeshProUGUI moveRightText;

    [Header("Reset")]
    public Button resetProgressButton;
    public GameObject confirmResetPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Back")]
    public Button backButton;

    [Header("Animation")]
    public float slideDistance = 800f;
    public float slideDuration = 0.15f;

    private string[] cardTitles = { "AUDIO", "DISPLAY", "KEYBINDINGS", "RESET" };
    private int currentCard = 0;
    private bool isAnimating = false;

    private List<GameObject> panels = new List<GameObject>();

    private const string MASTER_VOL = "MasterVolume";
    private const string MUSIC_VOL = "MusicVolume";
    private const string SFX_VOL = "SFXVolume";
    private const string FULLSCREEN = "Fullscreen";
    private const string RESOLUTION = "Resolution";

    private Resolution[] resolutions;

    private void Start()
    {
        // Make sure confirm panel is hidden on start
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);

        panels.Add(audioPanel);
        panels.Add(displayPanel);
        panels.Add(keybindingsPanel);
        panels.Add(resetPanel);

        LoadAudioSettings();
        SetupResolutionDropdown();
        SetupKeybindingsDisplay();
        WireButtons();

        ShowCard(0, false);
    }

    // ---- Card Navigation ----------------------------------------------------

    private void ShowCard(int index, bool animate = true)
    {
        if (isAnimating) return;

        int previousCard = currentCard;
        currentCard = index;

        foreach (var panel in panels)
            panel?.SetActive(false);

        panels[currentCard]?.SetActive(true);

        if (cardTitleText != null)
            cardTitleText.text = cardTitles[currentCard];

        if (pageIndicatorText != null)
            pageIndicatorText.text = (currentCard + 1) + " / " + cardTitles.Length;

        if (animate && cardPanel != null)
            StartCoroutine(SlideCard(index > previousCard ? 1 : -1));
    }

    private IEnumerator SlideCard(int direction)
    {
        isAnimating = true;

        Vector2 startPos = new Vector2(slideDistance * direction, 0f);
        Vector2 endPos = Vector2.zero;

        cardPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            cardPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        cardPanel.anchoredPosition = endPos;
        isAnimating = false;
    }

    private void GoLeft()
    {
        if (isAnimating) return;
        int index = currentCard - 1;
        if (index < 0) index = panels.Count - 1;
        ShowCard(index);
    }

    private void GoRight()
    {
        if (isAnimating) return;
        int index = currentCard + 1;
        if (index >= panels.Count) index = 0;
        ShowCard(index);
    }

    // ---- Audio --------------------------------------------------------------

    private void LoadAudioSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_VOL, 0.8f);
        float music = PlayerPrefs.GetFloat(MUSIC_VOL, 0.8f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOL, 0.8f);

        if (masterSlider != null) { masterSlider.value = master; masterSlider.onValueChanged.AddListener(SetMasterVolume); }
        if (musicSlider != null) { musicSlider.value = music; musicSlider.onValueChanged.AddListener(SetMusicVolume); }
        if (sfxSlider != null) { sfxSlider.value = sfx; sfxSlider.onValueChanged.AddListener(SetSFXVolume); }

        ApplyVolume(MASTER_VOL, master);
        ApplyVolume(MUSIC_VOL, music);
        ApplyVolume(SFX_VOL, sfx);
    }

    private void SetMasterVolume(float value) { ApplyVolume(MASTER_VOL, value); PlayerPrefs.SetFloat(MASTER_VOL, value); }
    private void SetMusicVolume(float value) { ApplyVolume(MUSIC_VOL, value); PlayerPrefs.SetFloat(MUSIC_VOL, value); }
    private void SetSFXVolume(float value) { ApplyVolume(SFX_VOL, value); PlayerPrefs.SetFloat(SFX_VOL, value); }

    private void ApplyVolume(string key, float value)
    {
        if (audioMixer == null) return;
        float db = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
        audioMixer.SetFloat(key, db);
    }

    // ---- Display ------------------------------------------------------------

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int currentIndex = 0;
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION, 0);
        var options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = savedIndex > 0 ? savedIndex : currentIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN, 1) == 1;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        Screen.fullScreen = isFullscreen;
    }

    private void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FULLSCREEN, value ? 1 : 0);
    }

    private void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(RESOLUTION, index);
    }

    // ---- Keybindings --------------------------------------------------------

    private void SetupKeybindingsDisplay()
    {
        if (moveUpText != null) moveUpText.text = "Move Up       Arrow Up / W";
        if (moveDownText != null) moveDownText.text = "Move Down     Arrow Down / S";
        if (moveLeftText != null) moveLeftText.text = "Move Left     Arrow Left / A";
        if (moveRightText != null) moveRightText.text = "Move Right    Arrow Right / D";
    }

    // ---- Reset & Back -------------------------------------------------------

    private void WireButtons()
    {
        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        backButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

        resetProgressButton?.onClick.AddListener(() =>
        {
            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(true);
        });

        confirmYesButton?.onClick.AddListener(() =>
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            if (PlayerUpgrades.Instance != null) Destroy(PlayerUpgrades.Instance.gameObject);
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(false);
        });

        confirmNoButton?.onClick.AddListener(() =>
        {
            if (confirmResetPanel != null)
                confirmResetPanel.SetActive(false);
        });
    }
}
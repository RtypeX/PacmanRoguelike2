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
        // INITIALIZE PANELS
        panels.Add(audioPanel);
        panels.Add(displayPanel);
        panels.Add(keybindingsPanel);
        panels.Add(resetPanel);

        // FORCE HIDE CONFIRMATION BOX
        if (confirmResetPanel != null)
            confirmResetPanel.SetActive(false);

        // SETUP SYSTEMS
        LoadAudioSettings();
        SetupResolutionDropdown();
        SetupKeybindingsDisplay();
        WireButtons();

        // SHOW FIRST CARD
        ShowCard(0, false);
    }

    // ---- Card Navigation ----------------------------------------------------

    private void ShowCard(int index, bool animate = true)
    {
        if (isAnimating) return;

        int previousCard = currentCard;
        currentCard = index;

        // Hide all cards
        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);

        // Show current card
        if (panels[currentCard] != null)
        {
            panels[currentCard].SetActive(true);

            // --- ADD THIS FIX HERE ---
            // If we are on the ResetCard (index 3), 
            // make sure the popup is hidden and the button is visible
            if (currentCard == 3 && confirmResetPanel != null)
            {
                confirmResetPanel.SetActive(false);
                resetProgressButton.gameObject.SetActive(true);
            }
        }

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

    private void GoLeft() { if (!isAnimating) ShowCard(currentCard - 1 < 0 ? panels.Count - 1 : currentCard - 1); }
    private void GoRight() { if (!isAnimating) ShowCard((currentCard + 1) % panels.Count); }

    // ---- Audio Logic --------------------------------------------------------

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
        float db = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        audioMixer.SetFloat(key, db);
    }

    // ---- Display Logic ------------------------------------------------------

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                currentResIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt(RESOLUTION, currentResIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        bool isFS = PlayerPrefs.GetInt(FULLSCREEN, 1) == 1;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFS;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void SetFullscreen(bool val) { Screen.fullScreen = val; PlayerPrefs.SetInt(FULLSCREEN, val ? 1 : 0); }
    private void SetResolution(int index) { Resolution res = resolutions[index]; Screen.SetResolution(res.width, res.height, Screen.fullScreen); PlayerPrefs.SetInt(RESOLUTION, index); }

    // ---- Keybindings --------------------------------------------------------

    private void SetupKeybindingsDisplay()
    {
        if (moveUpText != null) moveUpText.text = "Move Up: W / UpArrow";
        if (moveDownText != null) moveDownText.text = "Move Down: S / DownArrow";
        if (moveLeftText != null) moveLeftText.text = "Move Left: A / LeftArrow";
        if (moveRightText != null) moveRightText.text = "Move Right: D / RightArrow";
    }

    // ---- Final Implementation: Reset & Back ---------------------------------

    private void WireButtons()
    {
        leftArrowButton?.onClick.AddListener(GoLeft);
        rightArrowButton?.onClick.AddListener(GoRight);
        backButton?.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

        // Step 1: Show the confirmation box
        resetProgressButton?.onClick.AddListener(() => {
            if (confirmResetPanel != null) confirmResetPanel.SetActive(true);
        });

        // Step 2: Handle the "NO" - just hide the box
        confirmNoButton?.onClick.AddListener(() => {
            if (confirmResetPanel != null) confirmResetPanel.SetActive(false);
        });

        // Step 3: Handle the "YES" - perform the reset
        confirmYesButton?.onClick.AddListener(PerformFullReset);
    }

    private void PerformFullReset()
    {
        Debug.Log("Performing Progress Reset...");

        // Store current settings so they aren't lost
        float master = PlayerPrefs.GetFloat(MASTER_VOL, 0.8f);
        float music = PlayerPrefs.GetFloat(MUSIC_VOL, 0.8f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOL, 0.8f);
        int res = PlayerPrefs.GetInt(RESOLUTION, 0);
        int fs = PlayerPrefs.GetInt(FULLSCREEN, 1);

        // Clear everything
        PlayerPrefs.DeleteAll();

        // Restore Settings only
        PlayerPrefs.SetFloat(MASTER_VOL, master);
        PlayerPrefs.SetFloat(MUSIC_VOL, music);
        PlayerPrefs.SetFloat(SFX_VOL, sfx);
        PlayerPrefs.SetInt(RESOLUTION, res);
        PlayerPrefs.SetInt(FULLSCREEN, fs);
        PlayerPrefs.Save();

        // Destroy dynamic objects if they exist
        // Note: Replace "PlayerUpgrades" or "GameManager" with your actual script names
        GameObject pu = GameObject.Find("PlayerUpgrades"); // Or search for instance
        if (pu != null) Destroy(pu);

        // Hide panel and provide feedback
        if (confirmResetPanel != null) confirmResetPanel.SetActive(false);

        Debug.Log("Reset Complete. Settings Preserved.");
    }
}
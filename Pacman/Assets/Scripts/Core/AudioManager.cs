using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const float DefaultVolume = 0.8f;

    [Header("Resources Paths")]
    [SerializeField] private string menuMusicPath = "Audio/menu_music";
    [SerializeField] private string gameplayMusicPath = "Audio/gameplay_music";
    [SerializeField] private string pelletSfxPath = "Audio/pellet";
    [SerializeField] private string powerPelletSfxPath = "Audio/power_pellet";
    [SerializeField] private string fruitSfxPath = "Audio/fruit";
    [SerializeField] private string ghostEatenSfxPath = "Audio/ghost_eaten";
    [SerializeField] private string deathSfxPath = "Audio/death";
    [SerializeField] private string winSfxPath = "Audio/win";
    [SerializeField] private string loseSfxPath = "Audio/lose";

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip menuMusic;
    private AudioClip gameplayMusic;
    private AudioClip pelletSfx;
    private AudioClip powerPelletSfx;
    private AudioClip fruitSfx;
    private AudioClip ghostEatenSfx;
    private AudioClip deathSfx;
    private AudioClip winSfx;
    private AudioClip loseSfx;

    private readonly HashSet<string> missingClipWarnings = new HashSet<string>();
    private Coroutine resumeGameplayMusicCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
        LoadClips();
        RefreshVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    public void RefreshVolumes()
    {
        if (musicSource == null || sfxSource == null)
        {
            EnsureSources();
        }

        float master = PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);

        musicSource.volume = Mathf.Clamp01(master * music);
        sfxSource.volume = Mathf.Clamp01(master * sfx);
    }

    public void PlayPellet() => PlaySfx(pelletSfx, pelletSfxPath);
    public void PlayPowerPellet() => PlaySfx(powerPelletSfx, powerPelletSfxPath);
    public void PlayFruit() => PlaySfx(fruitSfx, fruitSfxPath);
    public void PlayGhostEaten() => PlaySfx(ghostEatenSfx, ghostEatenSfxPath);
    public void PlayDeath() => PlaySfx(deathSfx, deathSfxPath);
    public void PlayWin() => PlaySfx(winSfx, winSfxPath, false);
    public void PlayLose() => PlaySfx(loseSfx, loseSfxPath, false);

    public void PlayMenuMusic()
    {
        CancelGameplayResume();
        PlayMusic(menuMusic, menuMusicPath);
    }

    public void PlayGameplayMusic()
    {
        CancelGameplayResume();
        PlayMusic(gameplayMusic, gameplayMusicPath);
    }

    public void PlayMusicForScene(string sceneName)
    {
        if (IsGameplayScene(sceneName))
        {
            PlayGameplayMusic();
        }
        else
        {
            PlayMenuMusic();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void EnsureSources()
    {
        AudioSource[] existingSources = GetComponents<AudioSource>();
        musicSource = existingSources.Length > 0 ? existingSources[0] : gameObject.AddComponent<AudioSource>();
        sfxSource = existingSources.Length > 1 ? existingSources[1] : gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void LoadClips()
    {
        menuMusic = LoadClip(menuMusicPath);
        gameplayMusic = LoadClip(gameplayMusicPath);
        pelletSfx = LoadClip(pelletSfxPath);
        powerPelletSfx = LoadClip(powerPelletSfxPath);
        fruitSfx = LoadClip(fruitSfxPath);
        ghostEatenSfx = LoadClip(ghostEatenSfxPath);
        deathSfx = LoadClip(deathSfxPath);
        winSfx = LoadClip(winSfxPath);
        loseSfx = LoadClip(loseSfxPath);
    }

    private AudioClip LoadClip(string resourcePath)
    {
        return Resources.Load<AudioClip>(resourcePath);
    }

    private void PlayMusic(AudioClip clip, string resourcePath)
    {
        RefreshVolumes();

        if (clip == null)
        {
            clip = LoadClip(resourcePath);
        }

        if (clip == null)
        {
            WarnMissingClip(resourcePath);
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlaySfx(AudioClip clip, string resourcePath, bool resumeGameplayMusic = true)
    {
        RefreshVolumes();

        if (clip == null)
        {
            clip = LoadClip(resourcePath);
        }

        if (clip == null)
        {
            WarnMissingClip(resourcePath);
            return;
        }

        if (IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            PlayGameplaySingleChannel(clip, resumeGameplayMusic);
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    private void PlayGameplaySingleChannel(AudioClip clip, bool resumeGameplayMusic)
    {
        CancelGameplayResume();

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = false;
        musicSource.Play();

        if (resumeGameplayMusic && gameplayMusic != null)
        {
            resumeGameplayMusicCoroutine = StartCoroutine(ResumeGameplayMusicAfter(clip.length));
        }
    }

    private System.Collections.IEnumerator ResumeGameplayMusicAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        resumeGameplayMusicCoroutine = null;

        if (IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            PlayGameplayMusic();
        }
    }

    private void CancelGameplayResume()
    {
        if (resumeGameplayMusicCoroutine == null)
        {
            return;
        }

        StopCoroutine(resumeGameplayMusicCoroutine);
        resumeGameplayMusicCoroutine = null;
    }

    private void WarnMissingClip(string resourcePath)
    {
        if (missingClipWarnings.Add(resourcePath))
        {
            Debug.LogWarning($"AudioManager could not find an AudioClip at Resources/{resourcePath}.");
        }
    }

    // Fixed: avoid compile error when GameManager doesn't expose 'levelScenePrefix'.
    // Prefer using an explicit prefix string; if GameManager exposes a 'gameSceneName'
    // we derive a prefix from it (text up to last space). Otherwise fallback to "Level "
    private bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        string levelPrefix = "Level ";

        if (GameManager.Instance != null)
        {
            // Try to use public property 'levelScenePrefix' if it exists (reflectively).
            var gmType = GameManager.Instance.GetType();
            var field = gmType.GetField("levelScenePrefix");
            if (field != null)
            {
                var val = field.GetValue(GameManager.Instance) as string;
                if (!string.IsNullOrEmpty(val))
                {
                    levelPrefix = val;
                }
            }
            else
            {
                // Fallback: use gameSceneName (e.g. "Level 0") and take prefix up to last space
                var prop = gmType.GetProperty("gameSceneName");
                if (prop != null)
                {
                    var gs = prop.GetValue(GameManager.Instance) as string;
                    if (!string.IsNullOrEmpty(gs))
                    {
                        int lastSpace = gs.LastIndexOf(' ');
                        if (lastSpace >= 0)
                            levelPrefix = gs.Substring(0, lastSpace + 1);
                        else
                            levelPrefix = gs;
                    }
                }
            }
        }

        return sceneName.StartsWith(levelPrefix);
    }
}

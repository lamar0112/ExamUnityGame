using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Tildeler TMP default font til alle TextMeshProUGUI under dette objektet (unngår «No Font Asset»).</summary>
[DefaultExecutionOrder(-200)]
public class TmpDefaultFontOnAwake : MonoBehaviour
{
    private void Awake()
    {
        var def = TMP_Settings.defaultFontAsset;
        if (def == null) return;
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.font == null)
                tmp.font = def;
        }
    }
}

/// <summary>Lyd — pensum: AudioSource, AudioClip, PlayOneShot.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    public AudioClip jumpClip;
    public AudioClip collectOrbClip;
    public AudioClip damageClip;
    public AudioClip powerupClip;
    public AudioClip checkpointClip;
    public AudioClip levelCompleteClip;
    public AudioClip enemyDeathClip;
    public AudioClip menuClickClip;
    public AudioClip portalClip;
    public AudioClip backgroundMusic;

    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = sfxVolume;
        }
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayJump() => PlaySFX(jumpClip);
    public void PlayCollectOrb() => PlaySFX(collectOrbClip);
    public void PlayDamage() => PlaySFX(damageClip);
    public void PlayPowerup() => PlaySFX(powerupClip);
    public void PlayCheckpoint() => PlaySFX(checkpointClip);
    public void PlayLevelComplete() => PlaySFX(levelCompleteClip);
    public void PlayEnemyDeath() => PlaySFX(enemyDeathClip);
    public void PlayMenuClick() => PlaySFX(menuClickClip);
    public void PlayPortal() => PlaySFX(portalClip);
}

/// <summary>Ikke pensum ScriptableObject — bonus i rapporten.</summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "ExamGame/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName = "Character";
    [TextArea] public string description = "";
    public Sprite icon;
    public int maxHealth = 3;
    [Range(0.5f, 2f)] public float speedMultiplier = 1f;
    [Range(0.5f, 2f)] public float jumpMultiplier = 1f;
    public GameObject characterPrefab;
}

/// <summary>HUD — pensum: Canvas, TMP, Image.</summary>
public class HUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI orbsText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image[] heartIcons;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image powerupIcon;
    [SerializeField] private TextMeshProUGUI powerupNameText;
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnHealthChanged.AddListener(UpdateHealth);

        UpdateScore(0);
        UpdateOrbs(0);
        HidePowerup();

        if (playerHealth != null)
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void Update()
    {
        if (timerText != null && LevelTimer.Instance != null)
            timerText.text = LevelTimer.Instance.GetFormattedTime();

        UpdateScore(GameManager.Instance != null ? GameManager.Instance.Score : 0);
        UpdateOrbs(GameManager.Instance != null ? GameManager.Instance.OrbsCollected : 0);
    }

    private void UpdateScore(int s)
    {
        if (scoreText != null) scoreText.text = $"Score: {s}";
    }

    private void UpdateOrbs(int o)
    {
        if (orbsText != null) orbsText.text = $"Orbs: {o}";
    }

    private void UpdateHealth(int current, int max)
    {
        if (healthText != null) healthText.text = $"HP: {current}/{max}";
        if (heartIcons == null) return;
        for (int i = 0; i < heartIcons.Length; i++)
            if (heartIcons[i] != null)
                heartIcons[i].enabled = i < current;
    }

    public void ShowPowerup(string name, Sprite icon)
    {
        if (powerupIcon != null)
        {
            powerupIcon.gameObject.SetActive(true);
            if (icon != null) powerupIcon.sprite = icon;
        }
        if (powerupNameText != null)
        {
            powerupNameText.gameObject.SetActive(true);
            powerupNameText.text = name;
        }
    }

    public void HidePowerup()
    {
        powerupIcon?.gameObject.SetActive(false);
        powerupNameText?.gameObject.SetActive(false);
    }
}

/// <summary>Hovedmeny — start + avslutt.</summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;

    public void OnStartGame()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.ResetLevelStats();
        GameManager.Instance?.LoadScene("Level01");
    }

    public void OnShowControls()
    {
        AudioManager.Instance?.PlayMenuClick();
        mainPanel?.SetActive(false);
        controlsPanel?.SetActive(true);
    }

    public void OnHideControls()
    {
        AudioManager.Instance?.PlayMenuClick();
        controlsPanel?.SetActive(false);
        mainPanel?.SetActive(true);
    }

    public void OnQuit()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.QuitGame();
    }
}

/// <summary>Pause — Escape.</summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                OnResume();
            else
                OnPause();
        }
    }

    private void OnPause()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.PauseGame();
        pausePanel?.SetActive(true);
    }

    public void OnResume()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.ResumeGame();
        pausePanel?.SetActive(false);
    }

    public void OnRestartLevel()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.ResumeGame();
        GameManager.Instance?.RestartLevel();
    }

    public void OnMainMenu()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.ResumeGame();
        GameManager.Instance?.LoadScene("MainMenu");
    }

    public void OnQuit()
    {
        AudioManager.Instance?.PlayMenuClick();
        GameManager.Instance?.QuitGame();
    }
}

/// <summary>Level fullført-panel.</summary>
public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI orbsText;
    [SerializeField] private TextMeshProUGUI timeText;

    private void Start() => panel?.SetActive(false);

    public void Show(int score, int orbs, float time)
    {
        panel?.SetActive(true);
        Time.timeScale = 0f;

        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (orbsText != null) orbsText.text = $"Orbs: {orbs}";
        if (timeText != null)
        {
            int min = (int)(time / 60);
            int sec = (int)(time % 60);
            timeText.text = $"Time: {min:00}:{sec:00}";
        }
    }

    public void OnContinue()
    {
        AudioManager.Instance?.PlayMenuClick();
        Time.timeScale = 1f;
        string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string next = current switch
        {
            "Level01" => "Level02",
            "Level02" => "Level03",
            _ => "MainMenu"
        };
        GameManager.Instance?.LoadScene(next);
    }

    public void OnRestartLevel()
    {
        AudioManager.Instance?.PlayMenuClick();
        Time.timeScale = 1f;
        GameManager.Instance?.RestartLevel();
    }

    public void OnMainMenu()
    {
        AudioManager.Instance?.PlayMenuClick();
        Time.timeScale = 1f;
        GameManager.Instance?.LoadScene("MainMenu");
    }
}

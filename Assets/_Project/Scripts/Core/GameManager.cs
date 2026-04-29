using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton som lever mellom scener. Score, orbs, pause, scene load.
/// Pensum: C#, SceneManager, DontDestroyOnLoad, Time.timeScale.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int score;
    private int orbsCollected;
    private int enemiesDefeated;
    private bool isPaused;

    public bool IsPaused => isPaused;
    public int Score => score;
    public int OrbsCollected => orbsCollected;

    public CharacterData SelectedCharacter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad krever rot-GameObject (Unity-advarsel hvis under f.eks. ExamGreybox).
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureMainCamera();

    /// <summary>Øker orb-teller og score (brukes av Collectible).</summary>
    public void AddOrb(int pointsForThisPickup = 10)
    {
        orbsCollected++;
        score += pointsForThisPickup;
    }

    public void AddScore(int amount) => score += amount;

    public void RegisterEnemyDefeated()
    {
        enemiesDefeated++;
        score += 25;
    }

    public void ResetLevelStats()
    {
        score = 0;
        orbsCollected = 0;
        enemiesDefeated = 0;
    }

    public int GetEnemiesDefeated() => enemiesDefeated;

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void LoadScene(string sceneName)
    {
        ResumeGame();
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' er ikke i Build Settings. Kjør Exam/Setup Build Scenes.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void RestartLevel()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        ResumeGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetSelectedCharacter(CharacterData data)
    {
        SelectedCharacter = data;
        if (data != null)
            PlayerPrefs.SetString("LastCharacter", data.characterName);
    }

    private void EnsureMainCamera()
    {
        if (Camera.main != null) return;

        Camera anyEnabledCamera = FindFirstObjectByType<Camera>();
        if (anyEnabledCamera != null)
        {
            anyEnabledCamera.tag = "MainCamera";
            anyEnabledCamera.enabled = true;
            return;
        }

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camObj.AddComponent<AudioListener>();
        var follow = camObj.AddComponent<CameraFollow>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            follow.SetTarget(player.transform);
    }
}

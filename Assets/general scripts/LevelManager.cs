using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>Singleton that manages level loading, lives, game-over/complete flow, ghost spawning, and score persistence.</summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Level Konfiguration")]
    public string[] levelScenes;
    public int currentLevelIndex = 0;

    [Header("UI Referenzen (Menu)")]
    public GameObject mainMenuCanvas;
    public string mainMenuSceneName = "MainMenu";
    public TextMeshProUGUI maxScoreText;

    [Header("Life Settings")]
    public int lives = 3;
    private int maxLives = 3;
    public GameObject[] heartIcons;

    public GameObject worldGameUI;
    public TMP_Text nextLevelText;
    public Button startGameButton;
    public TMP_InputField nameInput;
    public GameObject NextLevelScene;
    public GameObject GameCompleteScene;

    [Header("Game Referenzen")]
    public GameObject ghostPrefab;
    public PacManMovement2D player;
    public BloodCollector bloodCollector;
    public golden_blood_collector goldenBloodCollector;

    [Header("Game Over Settings")]
    public float gameOverDelay = 2f;
    private bool isGameOver = false;
    public GameObject GameOverScene;

    [Header("Audio")]
    public AudioClip levelCompleteSound;
    public AudioClip gameOverSound;
    public AudioClip hitSound;
    public AudioClip goldenHitSound;
    [Range(0f, 1f)] public float levelCompleteVolume = 1f;
    [Range(0f, 1f)] public float gameOverVolume = 1f;
    [Range(0f, 1f)] public float hitVolume = 1f;
    [Range(0f, 1f)] public float goldenHitVolume = 1f;
    private AudioSource audioSource;

    [Header("Gegner Tracking")]
    private int activeGhostsCount = 0;

    [Header("Hit Feedback")]
    [Tooltip("Ein UI-Image das den gesamten Screen abdeckt (Color: rot, Alpha: 0). Im worldGameUI Canvas anlegen.")]
    public Image redVignetteImage;

    [Tooltip("Die Haupt-Kamera des Spiels")]
    public Camera gameCamera;

    [Tooltip("Wie stark die Kamera wackelt (0.15 empfohlen)")]
    public float shakeStrength = 0.15f;

    [Tooltip("Wie lange der Shake dauert in Sekunden")]
    public float shakeDuration = 0.4f;

    [Tooltip("Wie lange die rote Vignette sichtbar ist in Sekunden")]
    public float vignetteDuration = 0.6f;

    // Scoreboard
    private ScoreboardClient scoreboardClient;
    private string playerId = "";
    private string playerName = "";

    private string currentLoadedLevel = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            lives = maxLives;
            if (GameOverScene != null) GameOverScene.SetActive(false);
            if (GameCompleteScene != null) GameCompleteScene.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void UpdateMenuText()
    {
        if (nextLevelText != null)
            nextLevelText.text = "Level: " + (currentLevelIndex + 1);

        if (maxScoreText != null)
            maxScoreText.text = "Score: " + BloodCollector.globalScore.ToString();
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelScenes.Length)
        {
            return;
        }
        StartCoroutine(LoadLevelCoroutine(levelScenes[levelIndex]));
    }

    private IEnumerator GameCompleteRoutine()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        scoreboardClient = FindObjectOfType<ScoreboardClient>();

        if (scoreboardClient == null)
        {
            scoreboardClient = gameObject.AddComponent<ScoreboardClient>();
            Debug.Log("ScoreboardClient automatisch hinzugefügt.");
        }

        playerId = PlayerPrefs.GetString("PlayerId", "");
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("PlayerId", playerId);
            PlayerPrefs.Save();
        }

        playerName = PlayerPrefs.GetString("PlayerName", "");

        if (nameInput != null)
        {
            nameInput.text = playerName;
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
            nameInput.onEndEdit.AddListener(SavePlayerName);
        }

        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);

        BloodCollector.globalScore = PlayerPrefs.GetInt("GlobalScore", 0);

        lives = maxLives;
        UpdateLifeUI();
        UpdateMenuText();
        ShowMainMenu();
        ValidateStartButton(playerName);

        if (redVignetteImage != null)
            redVignetteImage.color = new Color(1f, 0f, 0f, 0f);
    }

    private IEnumerator LoadLevelCoroutine(string sceneName)
    {
        if (!string.IsNullOrEmpty(currentLoadedLevel))
            yield return SceneManager.UnloadSceneAsync(currentLoadedLevel);

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        currentLoadedLevel = sceneName;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        yield return null;
        SpawnGhostsAtMarkers();
        AssignLevelReferences();
    }

    public void OnNameInputChanged(string input) => ValidateStartButton(input);

    void ValidateStartButton(string input)
    {
        if (startGameButton != null)
            startGameButton.interactable = !string.IsNullOrWhiteSpace(input);
    }

    private void SpawnGhostsAtMarkers()
    {
        activeGhostsCount = 0;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawn");
        foreach (GameObject sp in spawnPoints)
        {
            if (ghostPrefab != null)
            {
                GameObject newGhost = Instantiate(ghostPrefab, sp.transform.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(newGhost, SceneManager.GetSceneByName(currentLoadedLevel));

                activeGhostsCount++;

                GhostMovement gm = newGhost.GetComponent<GhostMovement>();
                if (gm != null) gm.personality = (GhostMovement.GhostPersonality)Random.Range(0, 4);
            }
        }
    }

    public void SavePlayerName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            playerName = newName;
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
    }

    public void ShowMainMenu()
    {
        NextLevelScene.SetActive(false);
        GameOverScene.SetActive(false);
        if (GameCompleteScene != null) GameCompleteScene.SetActive(false);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
        if (worldGameUI != null) worldGameUI.SetActive(false);
        if (player != null) player.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        SavePlayerName(nameInput.text);

        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (worldGameUI != null) worldGameUI.SetActive(true);
        if (player != null) player.gameObject.SetActive(true);

        if (currentLevelIndex >= levelScenes.Length)
        {
            currentLevelIndex = 0;
            PlayerPrefs.SetInt("CurrentLevelIndex", 0);
        }

        LoadLevel(currentLevelIndex);
    }

    private void AssignLevelReferences()
    {
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        ResetCharacter();

        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        Tilemap wallTilemap = null;
        Tilemap bloodTilemap = null;
        Tilemap goldenBloodTilemap = null;

        foreach (Tilemap tm in allTilemaps)
        {
            string n = tm.gameObject.name.ToLower();
            if (n.Contains("wall")) wallTilemap = tm;
            else if (n.Contains("golden")) goldenBloodTilemap = tm;
            else if (n.Contains("blood")) bloodTilemap = tm;
        }

        if (player != null) player.tilemap = wallTilemap;
        if (bloodCollector != null) { bloodCollector.bloodTilemap = bloodTilemap; bloodCollector.InitializeLevel(); }
        if (goldenBloodCollector != null) goldenBloodCollector.bloodTilemap = goldenBloodTilemap;

        GhostMovement[] allGhosts = FindObjectsOfType<GhostMovement>();
        foreach (GhostMovement g in allGhosts)
        {
            g.tilemap = wallTilemap;
            g.player = player.transform;
        }

        GameObject sp = GameObject.FindGameObjectWithTag("PlayerSpawn");
        if (sp != null && player != null) player.transform.position = sp.transform.position;
    }

    public void NewPlayer()
    {
        // New ID so the old scoreboard entry stays intact
        playerId = System.Guid.NewGuid().ToString();
        PlayerPrefs.SetString("PlayerId", playerId);

        playerName = "";
        PlayerPrefs.SetString("PlayerName", "");

        BloodCollector.globalScore = 0;
        PlayerPrefs.SetInt("GlobalScore", 0);

        currentLevelIndex = 0;
        PlayerPrefs.SetInt("CurrentLevelIndex", 0);

        PlayerPrefs.Save();

        if (nameInput != null) nameInput.text = "";
        ValidateStartButton("");
        UpdateMenuText();

        Debug.Log($"[LevelManager] Neuer Spieler erstellt. ID: {playerId}");
    }

    public void OnLevelComplete()
    {
        if (levelCompleteSound != null)
        {
            audioSource.ignoreListenerPause = true;
            audioSource.PlayOneShot(levelCompleteSound, levelCompleteVolume);
        }

        currentLevelIndex++;
        PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
        PlayerPrefs.Save();

        SendScoreUpdate();

        bool allLevelsCompleted = currentLevelIndex >= levelScenes.Length;

        if (allLevelsCompleted)
        {
            // All levels completed
            StartCoroutine(GameCompleteSequence());
        }
        else
        {
            // More levels remaining
            NextLevelScene.SetActive(true);
            StartCoroutine(LevelCompleteRoutine());
        }
    }

    private IEnumerator LevelCompleteRoutine()
    {
        player.StopImmediately();
        yield return new WaitForSecondsRealtime(gameOverDelay);
        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator GameCompleteSequence()
    {
        player.StopImmediately();
        if (worldGameUI != null) worldGameUI.SetActive(false);
        if (GameCompleteScene != null) GameCompleteScene.SetActive(true);

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(gameOverDelay);

        BloodCollector.ResetGlobalScore();
        PlayerPrefs.SetInt("GlobalScore", 0);
        PlayerPrefs.Save();

        currentLevelIndex = 0;
        PlayerPrefs.SetInt("CurrentLevelIndex", 0);
        PlayerPrefs.Save();

        lives = maxLives;
        UpdateLifeUI();

        if (!string.IsNullOrEmpty(currentLoadedLevel))
        {
            Time.timeScale = 1f;
            yield return SceneManager.UnloadSceneAsync(currentLoadedLevel);
            currentLoadedLevel = "";
        }

        Time.timeScale = 1f;
        UpdateMenuText();
        ShowMainMenu();
    }

    private void ResetCharacter()
    {
        if (player != null)
        {
            player.StopImmediately();

            lives = maxLives;
            UpdateLifeUI();

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (goldenBloodCollector != null)
                goldenBloodCollector.DeactivateGoldenMode();
        }
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(currentLoadedLevel))
        {
            yield return SceneManager.UnloadSceneAsync(currentLoadedLevel);
            currentLoadedLevel = "";
        }
        UpdateMenuText();
        ShowMainMenu();
    }

    public void UpdateLifeUI()
    {
        if (heartIcons == null || heartIcons.Length == 0) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
                heartIcons[i].SetActive(i < lives);
        }
    }

    public void OnPlayerHit(GameObject enemy)
    {
        if (isGameOver) return;

        if (goldenBloodCollector != null && goldenBloodCollector.invincible)
        {
            if (goldenHitSound != null)
                audioSource.PlayOneShot(goldenHitSound, goldenHitVolume);

            if (enemy != null)
            {
                Destroy(enemy);
                activeGhostsCount--;
            }

            if (lives < 3) lives++;
            UpdateLifeUI();

            CheckLevelCompletion();
            return;
        }

        lives--;
        UpdateLifeUI();

        if (hitSound != null)
            audioSource.PlayOneShot(hitSound, hitVolume);

        if (enemy != null)
        {
            Destroy(enemy);
            activeGhostsCount--;
        }

        SendScoreUpdate();

        StartCoroutine(RedVignetteEffect());
        StartCoroutine(CameraShake());

        if (lives <= 0)
        {
            StartCoroutine(GameOverSequence());
        }
        else
        {
            CheckLevelCompletion();
        }
    }

    private void CheckLevelCompletion()
    {
        if (activeGhostsCount <= 0)
        {
            OnLevelComplete();
        }
    }

    private void SendScoreUpdate()
    {
        int currentScore = BloodCollector.globalScore;

        PlayerPrefs.SetInt("GlobalScore", currentScore);
        PlayerPrefs.Save();

        if (scoreboardClient == null)
        {
            Debug.LogWarning("ScoreboardClient nicht gefunden – Score wird nicht gesendet.");
            return;
        }

        scoreboardClient.AddScore(playerId, playerName, currentScore);
        Debug.Log($"Score gesendet: [{playerId}] {playerName} = {currentScore}");
    }

    private IEnumerator RedVignetteEffect()
    {
        if (redVignetteImage == null) yield break;

        float elapsed = 0f;
        float halfDuration = vignetteDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.75f, elapsed / halfDuration);
            redVignetteImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.75f, 0f, elapsed / halfDuration);
            redVignetteImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        redVignetteImage.color = new Color(1f, 0f, 0f, 0f);
    }

    private IEnumerator CameraShake()
    {
        if (gameCamera == null) yield break;

        Vector3 originalPos = gameCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = shakeStrength * (1f - (elapsed / shakeDuration));

            float offsetX = Random.Range(-1f, 1f) * strength;
            float offsetY = Random.Range(-1f, 1f) * strength;

            gameCamera.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        gameCamera.transform.localPosition = originalPos;
    }

    private IEnumerator GameOverSequence()
    {
        player.StopImmediately();
        isGameOver = true;

        if (gameOverSound != null)
            audioSource.PlayOneShot(gameOverSound, gameOverVolume);

        GameOverScene.SetActive(true);
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(gameOverDelay);

        SendScoreUpdate();

        BloodCollector.ResetGlobalScore();
        PlayerPrefs.SetInt("GlobalScore", 0);
        PlayerPrefs.Save();

        currentLevelIndex = 0;
        PlayerPrefs.SetInt("CurrentLevelIndex", 0);
        PlayerPrefs.Save();

        lives = maxLives;
        isGameOver = false;

        UpdateLifeUI();

        StartCoroutine(ReturnToMenuRoutine());
        currentLoadedLevel = "";
    }
}
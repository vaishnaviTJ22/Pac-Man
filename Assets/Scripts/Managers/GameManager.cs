using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public string playerName = "Player";
    private const string PLAYER_NAME_KEY = "PlayerName";
    public int score = 0;
    public int overallScore = 0;
    public int lives = 3;
    public int initialLives = 3;
    public int currentLevel = 1;

    [Header("References")]
    public PlayerController player;
    public GhostManager ghostManager;
    public MazeGenerator mazeGenerator;
    public GameObject leaderBoardPanel;

    [Header("Settings")]
    public float resetDelay = 2f;
    public float fruitDuration = 9f;
    public string leaderboardSceneName = "Leaderboard";

    [Header("Difficulty Scaling")]
    public GameDifficultyData difficultyData;

    [Header("Fruit Prefabs")]
    public GameObject cherryPrefab;
    public GameObject strawberryPrefab;

    private int dotsEaten = 0;
    public int totalDots = 0;
    public bool levelWon = false;
    private bool isResetting = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPlayerName();
    }

    private void LoadPlayerName()
    {
        if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
        {
            playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
            Debug.Log($"[GameManager] Loaded Player Name: {playerName}");
        }
    }

    public void SavePlayerName(string name)
    {
        playerName = name;
        PlayerPrefs.SetString(PLAYER_NAME_KEY, name);
        PlayerPrefs.Save();
        Debug.Log($"[GameManager] Saved Player Name: {name}");
    }

    void Start()
    {
        ResetGame();
    }

    public void ResetGame(bool fullRestart = false)
    {
        if (leaderBoardPanel != null) leaderBoardPanel.SetActive(false);
        
        score = 0;
        dotsEaten = 0;
        levelWon = false;
        
        if (fullRestart)
        {
            currentLevel = 1;
            overallScore = 0;
            lives = initialLives;
            if (difficultyData != null) difficultyData.ResetData();
        }
        
        // In-place reset logic
        if (mazeGenerator != null) mazeGenerator.Generate();
        if (player != null) 
        {
            player.ResetToStartPosition();
            player.Revive();
        }
        if (ghostManager != null) ghostManager.ResetAllGhosts();

        Debug.Log($"[GameManager] Game Reset. Level: {currentLevel}, Overall Score: {overallScore}");
    }

    public float GetPlayerSpeed()
    {
        return difficultyData != null ? difficultyData.GetPlayerSpeed() : 5f;
    }

    public float GetEnemySpeed()
    {
        return difficultyData != null ? difficultyData.GetEnemySpeed() : 4f;
    }

    public float GetPowerUpInterval()
    {
        return difficultyData != null ? difficultyData.GetPowerUpInterval() : 10f;
    }

    public float GetPowerUpDuration()
    {
        return difficultyData != null ? difficultyData.GetPowerUpDuration() : 8f;
    }

    public void IncrementLevel()
    {
        currentLevel++;
        if (difficultyData != null) difficultyData.IncrementLevel();
        
        ResetGame(false); // Start next level in-place
        Debug.Log($"[GameManager] Level Up! New Level: {currentLevel}");
    }
    public void HandlePlayerDeath()
    {
        if (isResetting) return;
        
        lives--;
        Debug.Log($"[GameManager] Life lost! Lives remaining: {lives}");

        if (lives > 0)
        {
            StartCoroutine(ResetRoundRoutine());
        }
        else
        {
            GameOver();
        }
    }

    private IEnumerator ResetRoundRoutine()
    {
        isResetting = true;
        
        if (player != null) player.OnCaughtByGhost();

        yield return new WaitForSeconds(resetDelay);

        if (player != null) player.ResetToStartPosition();
        if (ghostManager != null) ghostManager.ResetAllGhosts();

        if (player != null) player.Revive();

        isResetting = false;
        Debug.Log("[GameManager] Round Reset Complete.");
    }

    private void GameOver()
    {
        Debug.Log("[GameManager] GAME OVER!");
        levelWon = false;
        
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddScore(playerName, overallScore + score);
        }

        if (leaderBoardPanel != null) leaderBoardPanel.SetActive(true);
    }

    public void LevelCompleted()
    {
        Debug.Log("[GameManager] LEVEL COMPLETED!");
        levelWon = true;
        overallScore += score; // Add current level score to overall total

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddScore(playerName, overallScore);
        }

        if (leaderBoardPanel != null) leaderBoardPanel.SetActive(true);
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log($"[GameManager] Score: {score}");
    }

    public void OnDotEaten()
    {
        dotsEaten++;
        AddScore(10);

        if (dotsEaten == 70)
        {
            SpawnFruit(cherryPrefab);
        }
        else if (dotsEaten == 170)
        {
            SpawnFruit(strawberryPrefab);
        }

        if (dotsEaten >= totalDots && totalDots > 0)
        {
            LevelCompleted();
        }
    }

    private void SpawnFruit(GameObject prefab)
    {
        if (prefab == null || ghostManager == null) return;
        
        Vector3 spawnPos = ghostManager.ghostHouseExit != null 
            ? ghostManager.ghostHouseExit.position 
            : Vector3.zero;

        GameObject fruit = Instantiate(prefab, spawnPos, Quaternion.identity);
      
    }
}

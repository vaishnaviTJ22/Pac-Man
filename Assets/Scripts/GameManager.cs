using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages global game state: Score, Lives, and Round Resets.
/// Attach to a single GameObject in your scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int score = 0;
    public int lives = 3;
    public int initialLives = 3;

    [Header("References")]
    public PlayerController player;
    public GhostManager ghostManager;

    [Header("Settings")]
    public float resetDelay = 2f;
    public float fruitDuration = 9f;

    [Header("Fruit Prefabs")]
    public GameObject cherryPrefab;
    public GameObject strawberryPrefab;

    private int dotsEaten = 0;
    private bool isResetting = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Keep across scenes
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        score = 0;
        lives = initialLives;
        dotsEaten = 0;
        // Update UI here if you have one
    }

    /// <summary>Called when Pac-Man is caught by a ghost.</summary>
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
        
        // Pause movement
        if (player != null) player.OnCaughtByGhost();

        yield return new WaitForSeconds(resetDelay);

        // Reset positions
        if (player != null) player.ResetToStartPosition();
        if (ghostManager != null) ghostManager.ResetAllGhosts();

        // Revive player
        if (player != null) player.Revive();

        isResetting = false;
        Debug.Log("[GameManager] Round Reset Complete.");
    }

    private void GameOver()
    {
        Debug.Log("[GameManager] GAME OVER!");
        // Reload scene for now, or show UI
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log($"[GameManager] Score: {score}");
    }

    public void OnDotEaten()
    {
        dotsEaten++;
        AddScore(10); // Standard dot score

        if (dotsEaten == 70)
        {
            SpawnFruit(cherryPrefab);
        }
        else if (dotsEaten == 170)
        {
            SpawnFruit(strawberryPrefab);
        }
    }

    private void SpawnFruit(GameObject prefab)
    {
        if (prefab == null || ghostManager == null) return;
        
        Vector3 spawnPos = ghostManager.ghostHouseExit != null 
            ? ghostManager.ghostHouseExit.position 
            : Vector3.zero;

        GameObject fruit = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Add BonusFruit component if not present
        if (!fruit.GetComponent<BonusFruit>())
        {
            fruit.AddComponent<BonusFruit>();
        }
    }
}

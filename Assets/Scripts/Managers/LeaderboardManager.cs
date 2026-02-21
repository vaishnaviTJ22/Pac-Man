using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private string filePath;
    private LeaderboardData leaderboardData = new LeaderboardData();

    public int maxEntries = 10;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
        LoadLeaderboard();
    }

    public void AddScore(string playerName, int score)
    {
        if (string.IsNullOrEmpty(playerName)) playerName = "Anonymous";

        leaderboardData.entries.Add(new LeaderboardEntry(playerName, score));
        
        // Sort descending and keep top X
        leaderboardData.entries = leaderboardData.entries
            .OrderByDescending(e => e.score)
            .Take(maxEntries)
            .ToList();

        SaveLeaderboard();
    }

    public List<LeaderboardEntry> GetEntries()
    {
        return leaderboardData.entries;
    }

    private void SaveLeaderboard()
    {
        string json = JsonUtility.ToJson(leaderboardData, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"[LeaderboardManager] Saved to {filePath}");
    }

    private void LoadLeaderboard()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
            Debug.Log("[LeaderboardManager] Loaded data.");
        }
        else
        {
            leaderboardData = new LeaderboardData();
            Debug.Log("[LeaderboardManager] No file found, starting fresh.");
        }
    }

    public void LoadLoginScene()
    {
        SceneManager.LoadScene("Login");
    }
}

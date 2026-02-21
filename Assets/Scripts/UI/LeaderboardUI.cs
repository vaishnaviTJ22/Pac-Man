using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject entryPrefab;
    public Transform entryContainer;
    public string mainMenuSceneName = "Login"; // Based on existing scenes

    void OnEnable()
    {
        DisplayLeaderboard();
    }

    public void DisplayLeaderboard()
    {
        if (entryContainer == null || entryPrefab == null) return;

        // Clear existing entries
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogError("Error: Leaderboard Manager not found.");
            return;
        }

        List<LeaderboardEntry> entries = LeaderboardManager.Instance.GetEntries();
        
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("No scores yet!");
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject entryObj = Instantiate(entryPrefab, entryContainer);
            LeaderboardEntryUI entryUI = entryObj.GetComponent<LeaderboardEntryUI>();
            
            if (entryUI != null)
            {
                entryUI.SetEntry(i + 1, entries[i].playerName, entries[i].score);
            }
        }
    }

    public void PlayAgain()
    {
        if (GameManager.Instance != null)
        {
            // This will increment level and reload the scene
            GameManager.Instance.IncrementLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ReturnToMainMenu()
    {
       
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

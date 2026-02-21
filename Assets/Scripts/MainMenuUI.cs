using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button startButton;
    public TextMeshProUGUI warningText;

    [Header("Settings")]
    public string gameSceneName = "GamePlay";

    void Start()
    {
        // Ensure warning is hidden at start
        if (warningText != null)
            warningText.gameObject.SetActive(false);
            
        // Start button should be interactable by default
        if (startButton != null)
            startButton.interactable = true;
    }

    public void StartGame()
    {
        string playerName = nameInputField != null ? nameInputField.text : "";
        
        // Validate name on click
        if (string.IsNullOrWhiteSpace(playerName))
        {
            if (warningText != null)
                warningText.gameObject.SetActive(true);
            
            Debug.Log("[MainMenuUI] Name is empty. Showing warning.");
            return;
        }

        // If name is valid, proceed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerName = playerName;
        }

        Debug.Log($"[MainMenuUI] Starting game with player: {playerName}");
        SceneManager.LoadScene(gameSceneName);
    }
}

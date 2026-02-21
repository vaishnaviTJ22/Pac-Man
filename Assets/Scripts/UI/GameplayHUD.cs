using UnityEngine;
using TMPro;

public class GameplayHUD : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI lifeText;

    void Start()
    {
        UpdateHUD();
    }

    void Update()
    {
        // Polling score for now, can be optimized with events later
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (GameManager.Instance != null)
        {
            if (nameText != null)
                nameText.text = "Player: " + GameManager.Instance.playerName;
            
            if (scoreText != null)
                scoreText.text = "Score: " + GameManager.Instance.score.ToString();

            if(lifeText != null)
                lifeText.text="Life: "+GameManager.Instance.lives.ToString();
        }
    }
}

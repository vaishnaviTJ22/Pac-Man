using UnityEngine;
using TMPro;

public class LeaderboardEntryUI : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;

    public void SetEntry(int rank, string playerName, int score)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (playerNameText != null) playerNameText.text = playerName;
        if (scoreText != null) scoreText.text = score.ToString();
    }
}

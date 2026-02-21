using UnityEngine;

[CreateAssetMenu(fileName = "GameDifficultyData", menuName = "PacMan/Difficulty Data")]
public class GameDifficultyData : ScriptableObject
{
    [Header("Current Level Info")]
    public int currentLevel = 1;

    [Header("Speed Settings")]
    public float basePlayerSpeed = 5f;
    public float baseEnemySpeed = 4f;
    public float speedIncreasePerLevel = 0.5f;

    [Header("Interval Settings")]
    public float basePowerUpInterval = 10f;
    public float intervalDecreasePerLevel = 0.5f;

    [Header("Power-Up Settings")]
    public float basePowerUpDuration = 10f;
    public float durationDecreasePerLevel = 0.5f;

    public void ResetData()
    {
        currentLevel = 1;
    }

    public void IncrementLevel()
    {
        currentLevel++;
    }

    public float GetPlayerSpeed()
    {
        return basePlayerSpeed + (currentLevel - 1) * speedIncreasePerLevel;
    }

    public float GetEnemySpeed()
    {
        return baseEnemySpeed + (currentLevel - 1) * speedIncreasePerLevel;
    }

    public float GetPowerUpInterval()
    {
        // Interval decreases to make it harder (powerups spawn less frequently or we wait longer?)
        // The user said "increase the value", maybe they want longer gaps?
        // Let's assume they want it to scale with difficulty.
        return Mathf.Max(2f, basePowerUpInterval + (currentLevel - 1) * intervalDecreasePerLevel);
    }

    public float GetPowerUpDuration()
    {
        return Mathf.Max(3f, basePowerUpDuration - (currentLevel - 1) * durationDecreasePerLevel);
    }
}

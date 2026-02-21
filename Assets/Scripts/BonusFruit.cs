using UnityEngine;

/// <summary>
/// Handles bonus fruit behavior: scoring and self-destruction.
/// </summary>
public class BonusFruit : MonoBehaviour
{
    public int scoreValue = 100; // Default for cherry, override in Inspector if needed
    public float duration = 9f;

    private void Start()
    {
        // Auto-destroy after duration
        Destroy(gameObject, duration);

        // Specific values based on name
        if (name.Contains("Cherry")) scoreValue = 100;
        else if (name.Contains("Strawberry")) scoreValue = 300;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
                Debug.Log($"[BonusFruit] {name} collected! +{scoreValue} points");
            }
            Destroy(gameObject);
        }
    }
}

using UnityEngine;

/// <summary>
/// Script for Booster Bottle and Health Orb items.
/// Attach to a prefab with a trigger collider.
/// </summary>
public class PowerUpItem : MonoBehaviour
{
    public enum PowerUpType { BoosterBottle, HealthOrb }
    
    [Header("Settings")]
    public PowerUpType type;
    public float duration = 5f;
    public float speedMultiplier = 1.5f;
    
    [Header("Visuals (Optional)")]
    public float rotationSpeed = 100f;

    void Update()
    {
        // Simple spin animation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ApplyEffect(player);
                Destroy(gameObject);
            }
        }
    }

    private void ApplyEffect(PlayerController player)
    {
        switch (type)
        {
            case PowerUpType.BoosterBottle:
                Debug.Log("[PowerUp] Booster Bottle collected! Speed UP + Wall Pass ON.");
                player.ApplyBoosterBottle(duration, speedMultiplier);
                break;
                
            case PowerUpType.HealthOrb:
                Debug.Log("[PowerUp] Health Orb collected! Invincibility ON.");
                player.ApplyHealthOrb(duration);
                break;
        }
    }
}

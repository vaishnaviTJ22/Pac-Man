using UnityEngine;

/// <summary>
/// Attach this to any energizer (power pellet) object in the scene.
/// When Pac-Man walks into it, all ghosts enter Frightened mode.
///
/// Setup:
/// 1. Create a small sphere or capsule in the maze at each power-pellet position.
/// 2. Add this script.
/// 3. Make the collider a Trigger (tick "Is Trigger" in the Collider component).
/// 4. Tag Pac-Man as "Player".
/// </summary>
public class EnergizerController : MonoBehaviour
{
    [Header("Visual Pulse")]
    [Tooltip("How fast the energizer pulses in scale")]
    public float pulseSpeed = 2f;
    [Tooltip("How much the energizer grows/shrinks")]
    public float pulseAmount = 0.15f;

    [Header("Score")]
    public int scoreValue = 50;

    private Vector3 baseScale;
    private Renderer energizerRenderer;

    void Start()
    {
        baseScale = transform.localScale;
        energizerRenderer = GetComponent<Renderer>();
        // Give it a bright white/yellow emissive look
        if (energizerRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard"));
            mat.color = Color.yellow;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.yellow * 0.8f);
            energizerRenderer.material = mat;
        }
    }

    void Update()
    {
        // Pulsing scale animation
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Notify GhostManager
        if (GhostManager.Instance != null)
            GhostManager.Instance.OnEnergizerEaten();

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        Debug.Log($"[Energizer] Collected! +{scoreValue} points");

        // Deactivate (hide) the energizer
        gameObject.SetActive(false);
    }
}

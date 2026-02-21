using UnityEngine;
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
        energizerRenderer = GetComponentInChildren<Renderer>();
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
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GhostManager.Instance != null)
            GhostManager.Instance.OnEnergizerEaten();

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue);

        Debug.Log($"[Energizer] Collected! +{scoreValue} points");

        gameObject.SetActive(false);
    }
}

using UnityEngine;

public class DotController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnDotEaten();

            Destroy(gameObject);
        }
    }
}

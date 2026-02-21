using UnityEngine;

public class DotController : MonoBehaviour
{
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

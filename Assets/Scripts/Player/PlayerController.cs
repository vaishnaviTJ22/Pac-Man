using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 nextDirection = Vector3.zero;
    private Vector3 targetPosition;
    
    [Header("Power-Up Status")]
    public bool isGhosting = false;       
    public bool isInvincible = false;    
    public float currentSpeedMultiplier = 1f;

    private Rigidbody rb;
    private MazeGenerator mazeGen;
    private bool isDead = false;
    private const float threshold = 0.05f; 
    
    private Coroutine speedCoroutine;
    private Coroutine ghostCoroutine;
    private Coroutine invincibilityCoroutine;

    private Renderer playerRenderer;
    private Color normalColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        mazeGen = FindFirstObjectByType<MazeGenerator>();
        
        if (mazeGen != null && mazeGen.mazeData != null)
        {
            transform.position = SnapToGrid(transform.position);
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = startPosition;

        rb.useGravity = false;
        rb.isKinematic = true; 

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            normalColor = playerRenderer.material.color;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) nextDirection = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) nextDirection = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) nextDirection = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) nextDirection = Vector3.right;

        if (nextDirection != Vector3.zero && nextDirection == -moveDirection)
        {
            targetPosition = targetPosition - moveDirection * (mazeGen ? mazeGen.mazeData.cellSize : 1f);
            
            moveDirection = nextDirection;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            
            Debug.Log($"[PlayerController] Reversing. New target: {targetPosition}");
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float speed = moveSpeed * currentSpeedMultiplier;
        if (GameManager.Instance != null)
            speed *= GameManager.Instance.GetSpeedMultiplier();
            
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (Vector3.Distance(transform.position, targetPosition) < threshold)
        {
            transform.position = targetPosition;

            if (nextDirection != Vector3.zero && CanMove(nextDirection))
            {
                moveDirection = nextDirection;
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            if (moveDirection != Vector3.zero && CanMove(moveDirection))
            {
                targetPosition += moveDirection * (mazeGen ? mazeGen.mazeData.cellSize : 1f);
            }
        }
    }

    private bool CanMove(Vector3 dir)
    {
        if (dir == Vector3.zero) return false;

        if (isGhosting) return true;

        if (mazeGen == null || mazeGen.mazeData == null) return false;

        float cs = mazeGen.mazeData.cellSize;
        
        Vector3 nextTargetPos = targetPosition + dir * cs;
        Vector2Int gridPos = WorldToGrid(nextTargetPos);
        MazeData.TileType tile = mazeGen.mazeData.GetCell(gridPos.x, gridPos.y);

        if (tile == MazeData.TileType.Wall)
        {
            return false;
        }

        Ray ray = new Ray(targetPosition, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, cs))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                return false;
            }
        }
        
        return true; 
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (mazeGen == null) return Vector2Int.zero;
        return mazeGen.WorldToGrid(worldPos);
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGrid(worldPos);
        return mazeGen.GridToWorld(gridPos.x, gridPos.y);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
    public void OnCaughtByGhost()
    {
        if (isDead || isInvincible) return; // Ignore if dead or has Health Orb powerup
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        moveDirection = Vector3.zero;
        Debug.Log("[PlayerController] Caught by ghost!");
        
        if (GameManager.Instance != null)
            GameManager.Instance.HandlePlayerDeath();
    }
    public void ResetToStartPosition()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        targetPosition = startPosition;
        moveDirection = Vector3.zero;
        nextDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    public void Revive()
    {
        isDead = false;
        StopAllPowerUps();
    }


    public void ApplyBoosterBottle(float duration, float speedMultiplier)
    {
        if (speedCoroutine != null) StopCoroutine(speedCoroutine);
        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
        
        speedCoroutine = StartCoroutine(SpeedBoostRoutine(duration, speedMultiplier));
        ghostCoroutine = StartCoroutine(GhostModeRoutine(duration));
    }

    public void ApplyHealthOrb(float duration)
    {
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        currentSpeedMultiplier = multiplier;
        UpdatePlayerColor();
        yield return new WaitForSeconds(duration);
        currentSpeedMultiplier = 1f;
        speedCoroutine = null;
        UpdatePlayerColor();
    }

    private System.Collections.IEnumerator GhostModeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        isGhosting = false;
        ghostCoroutine = null;
    }

    private System.Collections.IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        UpdatePlayerColor();
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        invincibilityCoroutine = null;
        UpdatePlayerColor();
    }

    private void UpdatePlayerColor()
    {
        if (playerRenderer == null) return;

        if (isInvincible)
        {
            playerRenderer.material.color = Color.green;
        }
        else if (speedCoroutine != null)
        {
            playerRenderer.material.color = Color.red;
        }
        else
        {
            playerRenderer.material.color = normalColor;
        }
    }

    private void StopAllPowerUps()
    {
        if (speedCoroutine != null) StopCoroutine(speedCoroutine);
        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        
        currentSpeedMultiplier = 1f;
        isGhosting = false;
        isInvincible = false;
        UpdatePlayerColor();
    }
}

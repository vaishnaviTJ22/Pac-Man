using UnityEngine;

/// <summary>
/// Pac-Man player movement — uses Rigidbody physics so walls block movement.
///
/// SETUP (Inspector):
/// 1. Tag this GameObject → "Player"
/// 2. Sphere Collider → Is Trigger = FALSE  (solid, for wall collision)
/// 3. Add Rigidbody:
///      - Use Gravity = false
///      - Is Kinematic  = false
///      - Freeze Position Y  = true
///      - Freeze Rotation X, Y, Z = true (all three)
///
/// Ghost / Energizer triggers still fire because their OWN colliders are triggers.
/// </summary>
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
    public bool isGhosting = false;       // Can pass through walls
    public bool isInvincible = false;     // Ghosts can't catch you
    public float currentSpeedMultiplier = 1f;

    private Rigidbody rb;
    private MazeGenerator mazeGen;
    private bool isDead = false;
    private const float threshold = 0.05f; // Precision for tile center snapping
    
    private Coroutine speedCoroutine;
    private Coroutine ghostCoroutine;
    private Coroutine invincibilityCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find the maze generator in the scene
        mazeGen = FindFirstObjectByType<MazeGenerator>();
        
        if (mazeGen != null && mazeGen.mazeData != null)
        {
            // Snap starting position to the nearest grid center
            transform.position = SnapToGrid(transform.position);
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = startPosition;

        rb.useGravity = false;
        rb.isKinematic = true; 
    }

    void Update()
    {
        if (isDead) return;

        // 1. Buffer Input (Stored until we reach a tile center)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) nextDirection = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) nextDirection = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) nextDirection = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) nextDirection = Vector3.right;

        // 2. Immediate Reversal (Special rule in Pac-Man)
        if (nextDirection != Vector3.zero && nextDirection == -moveDirection)
        {
            // Calculate previous center BEFORE updating moveDirection
            targetPosition = targetPosition - moveDirection * (mazeGen ? mazeGen.mazeData.cellSize : 1f);
            
            moveDirection = nextDirection;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            
            Debug.Log($"[PlayerController] Reversing. New target: {targetPosition}");
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 3. Move towards the target center
        float speed = moveSpeed * currentSpeedMultiplier;
        if (GameManager.Instance != null)
            speed *= GameManager.Instance.GetSpeedMultiplier();
            
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // 4. If we reached the target center, decide the next target
        if (Vector3.Distance(transform.position, targetPosition) < threshold)
        {
            // Snap exactly to avoid cumulative error
            transform.position = targetPosition;

            // Try to turn to buffered direction
            if (nextDirection != Vector3.zero && CanMove(nextDirection))
            {
                moveDirection = nextDirection;
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            // If we can keep moving in current direction, set new target
            if (moveDirection != Vector3.zero && CanMove(moveDirection))
            {
                targetPosition += moveDirection * (mazeGen ? mazeGen.mazeData.cellSize : 1f);
            }
        }
    }

    private bool CanMove(Vector3 dir)
    {
        if (dir == Vector3.zero) return false;

        // --- POWER UP Check: Booster Bottle (Wall Pass) ---
        if (isGhosting) return true;

        if (mazeGen == null || mazeGen.mazeData == null) return false;

        float cs = mazeGen.mazeData.cellSize;
        
        // --- CHECK 1: Grid Data ---
        Vector3 nextTargetPos = targetPosition + dir * cs;
        Vector2Int gridPos = WorldToGrid(nextTargetPos);
        MazeData.TileType tile = mazeGen.mazeData.GetCell(gridPos.x, gridPos.y);

        if (tile == MazeData.TileType.Wall)
        {
            // Debug.Log($"[PlayerController] GRID BLOCKED at {gridPos}");
            return false;
        }

        // --- CHECK 2: Physical Collision (The "Double-Check") ---
        // Raycast from current center to next center to catch any manual wall placement or rounding errors
        Ray ray = new Ray(targetPosition, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, cs))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                // Debug.Log($"[PlayerController] PHYSICAL BLOCKED by {hit.collider.name}");
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

    private Vector3 GetPreviousTileCenter()
    {
        float cs = (mazeGen != null && mazeGen.mazeData != null) ? mazeGen.mazeData.cellSize : 1f;
        return targetPosition - moveDirection * cs;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetPosition);
    }

    /// <summary>Called by GhostManager when a ghost catches Pac-Man.</summary>
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

    /// <summary>Resets the player to their initial position and rotation.</summary>
    public void ResetToStartPosition()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        targetPosition = startPosition;
        moveDirection = Vector3.zero;
        nextDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    /// <summary>Revive after respawn.</summary>
    public void Revive()
    {
        isDead = false;
        // Reset powerups on death/respawn
        StopAllPowerUps();
    }

    // ── Power-Up API ──────────────────────────────────────────────────────────

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
        yield return new WaitForSeconds(duration);
        currentSpeedMultiplier = 1f;
        speedCoroutine = null;
    }

    private System.Collections.IEnumerator GhostModeRoutine(float duration)
    {
       // isGhosting = true;
        // Optional: Change transparency here
        yield return new WaitForSeconds(duration);
        isGhosting = false;
        ghostCoroutine = null;
    }

    private System.Collections.IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        // Optional: Visual indicator (e.g. flashing)
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        invincibilityCoroutine = null;
    }

    private void StopAllPowerUps()
    {
        if (speedCoroutine != null) StopCoroutine(speedCoroutine);
        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        
        currentSpeedMultiplier = 1f;
        isGhosting = false;
        isInvincible = false;
    }
}

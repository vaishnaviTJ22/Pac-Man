using System.Collections;
using UnityEngine;

public enum GhostType { Blinky, Pinky, Inky, Clyde }

public enum GhostState
{
    InHouse,        // Bobbing inside ghost house before release
    ExitingHouse,   // Moving upward through ghost house door
    Chase,          // Hunting Pac-Man using unique per-ghost logic
    Scatter,        // Retreating to a corner of the maze
    Frightened,     // Pac-Man ate an energizer — ghost is edible
    Eaten           // Ghost was eaten, returning to house
}

public class GhostController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Ghost Identity")]
    public GhostType ghostType = GhostType.Blinky;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float frightenedSpeed = 2f;
    public float eatenSpeed = 8f;

    [Header("Wall Detection")]
    [Tooltip("Raycast distance used to detect walls ahead / to the sides")]
    public float wallCheckDistance = 0.6f;
    [Tooltip("Tag applied to your wall cubes — must match exactly")]
    public string wallTag = "Wall";

    [Header("Bobbing (In-House)")]
    public float bobAmount = 0.15f;
    public float bobSpeed = 2f;

    [Header("Scatter Corners")]
    [Tooltip("Assigned automatically by GhostManager, but can override here")]
    public Vector3 scatterTarget = Vector3.zero;

    // ── Runtime references (set by GhostManager) ─────────────────────────────
    public Transform playerTransform;
    public Vector3 houseCenter;
    public Vector3 houseExitPoint;
    public GhostManager manager;

    // ── State ─────────────────────────────────────────────────────────────────
    public GhostState currentState = GhostState.InHouse;
    private Vector3 startPosition;
    private Vector3 moveDir = Vector3.zero;

    // State Execution
    private System.Action activeUpdate;

    // Timers / helpers
    private float randomDirTimer = 0f;
    private const float RANDOM_DIR_INTERVAL = 1.5f;
    private const float WALL_RAY_UP_OFFSET = 0.2f;

    // Grid Movement
    private Vector3 targetNode;
    private bool isAtNode = true;

    // Frightened flash near end of mode
    private bool isFrightened => currentState == GhostState.Frightened;
    private Renderer ghostRenderer;
    [Header("Colors")]
    public Color normalColor;
    public Color frightenedColor = Color.blue;
    public Color flashColor = Color.white;
    private float frightenedTimeLeft = 0f;
    private const float FRIGHTENED_DURATION = 8f;
    private const float FLASH_START = 2f;

    // Eaten — remember where the house is to return to
    private bool returningToHouse = false;

    private MazeGenerator mazeGen;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        ghostRenderer = GetComponentInChildren<Renderer>();
        startPosition = transform.position;
        mazeGen = FindFirstObjectByType<MazeGenerator>();
    }

    void Start()
    {
        if (ghostRenderer != null)
        {
            // If normalColor is black or unassigned, try to capture it or set defaults
            if (IsColorBlackOrClear(normalColor))
                normalColor = ghostRenderer.material.color;
        }

        if (IsColorBlackOrClear(normalColor))
        {
            normalColor = GetClassicColor(ghostType);
            UpdateColor(normalColor);
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning($"[{ghostType}] No Player found! Tag Pac-Man as 'Player'.");
        }

        SetState(GhostState.InHouse);
    }

    void Update()
    {
        // Enforce Y height to prevent floor passthrough
        //transform.position = new Vector3(transform.position.x, startPosition.y, transform.position.z);

        // Execute active state update
        activeUpdate?.Invoke();
    }

    // ── Public API (called by GhostManager) ──────────────────────────────────

    public void Release()
    {
        if (currentState == GhostState.InHouse)
            SetState(GhostState.ExitingHouse);
    }

    public void EnterFrightenedMode()
    {
        if (currentState == GhostState.Eaten) return;  // Already eaten, skip

        frightenedTimeLeft = FRIGHTENED_DURATION;
        SetState(GhostState.Frightened);
        // Reverse direction immediately
        moveDir = -moveDir;
    }

    public void SetChaseMode(bool chase)
    {
        /* if (currentState == GhostState.InHouse ||
            // currentState == GhostState.ExitingHouse ||
             currentState == GhostState.Frightened ||
             currentState == GhostState.Eaten) return;*/
        Debug.Log("setchase mode");
        SetState(chase ? GhostState.Chase : GhostState.Scatter);
    }

    public void ResetToHouse()
    {
        transform.position = startPosition;
        moveDir = Vector3.zero;
        isAtNode = true;
        SetState(GhostState.InHouse);
    }

    private void SetState(GhostState newState)
    {
        currentState = newState;
        Debug.Log("current state : " + currentState);
        switch (newState)
        {
            case GhostState.InHouse:
                UpdateColor(normalColor);
                activeUpdate = UpdateInHouse;
                break;

            case GhostState.ExitingHouse:
                moveDir = Vector3.zero;
                activeUpdate = UpdateExiting;
                break;

            case GhostState.Chase:
                Debug.Log("chase mode");
                UpdateColor(normalColor);
                activeUpdate = () => UpdateGridMovement(GetChaseTarget(), moveSpeed);
                break;

            case GhostState.Scatter:
                if (moveDir == Vector3.zero)
                    moveDir = Vector3.forward;
                UpdateColor(normalColor);
                activeUpdate = () => UpdateGridMovement(scatterTarget, moveSpeed);
                break;

            case GhostState.Frightened:
                UpdateColor(frightenedColor);
                activeUpdate = UpdateFrightened;
                break;

            case GhostState.Eaten:
                UpdateColor(new Color(1f, 1f, 1f, 0.4f));
                returningToHouse = true;
                activeUpdate = UpdateEaten;
                break;
        }
    }
    private void UpdateInHouse()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void UpdateExiting()
    {
        Vector3 target = houseExitPoint;
        transform.position = Vector3.MoveTowards(transform.position, target,
                                                  moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Debug.Log("update exiting");
            transform.position = target;
            targetNode = target;
            isAtNode = true;
            moveDir = Vector3.forward; // Ensure we start with a valid direction
            SetState(GhostState.Chase);
        }
    }

    private void UpdateGridMovement(Vector3 finalTarget, float speed)
    {
        if (isAtNode)
        {
            moveDir = DecideNextDirection(finalTarget);
            float cs = (manager != null && manager.mazeData != null ? manager.mazeData.cellSize : 1f);
            targetNode = transform.position + moveDir * cs;
            isAtNode = false;
        }

        float currentSpeed = speed;
        if (GameManager.Instance != null)
            currentSpeed *= GameManager.Instance.GetSpeedMultiplier();

        transform.position = Vector3.MoveTowards(transform.position, targetNode, currentSpeed * Time.deltaTime);

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.deltaTime);
        }
        if (Vector3.Distance(transform.position, targetNode) < 0.05f)
        {
            transform.position = targetNode; // Snap exactly
            isAtNode = true;
        }
    }

    private void UpdateFrightened()
    {
        frightenedTimeLeft -= Time.deltaTime;
        if (frightenedTimeLeft <= FLASH_START)
        {
            bool flashOn = Mathf.FloorToInt(frightenedTimeLeft * 4) % 2 == 0;
            UpdateColor(flashOn ? flashColor : frightenedColor);
        }

        if (frightenedTimeLeft <= 0f)
        {
            bool globalChase = manager != null && manager.IsChaseMode;
            SetState(globalChase ? GhostState.Chase : GhostState.Scatter);
            return;
        }
        UpdateGridMovement(transform.position + GetSafeRandomDirection(moveDir) * 5f, frightenedSpeed);
    }

    private void UpdateEaten()
    {
        UpdateGridMovement(houseCenter, eatenSpeed);

        if (Vector3.Distance(transform.position, houseCenter) < 0.2f)
        {
            transform.position = startPosition;
            returningToHouse = false;
            SetState(GhostState.InHouse);
            if (manager != null) manager.ScheduleRespawn(this);
        }
    }
    private Vector3 GetChaseTarget()
    {
        Debug.Log("getchasetarget");
        if (playerTransform == null) return transform.position;
        Debug.Log("player set");
        switch (ghostType)
        {
            // ── Blinky: directly target Pac-Man's position ─────────────────
            case GhostType.Blinky:
                return playerTransform.position;

            // ── Pinky: 4 units ahead of Pac-Man's facing direction ─────────
            case GhostType.Pinky:
                {
                    Vector3 pacForward = playerTransform.forward;
                    // Clamp to nearest axis (arcade accuracy)
                    pacForward = SnapToAxis(pacForward);
                    return playerTransform.position + pacForward * 4f;
                }

            // ── Inky: hybrid — sometimes random, sometimes targeting ────────
            case GhostType.Inky:
                {
                    randomDirTimer -= Time.deltaTime;
                    if (randomDirTimer <= 0f)
                    {
                        randomDirTimer = RANDOM_DIR_INTERVAL;
                        // 50% chance to target Pac-Man, 50% random wander
                        if (Random.value < 0.5f)
                            return playerTransform.position;
                        else
                            return transform.position + GetSafeRandomDirection(moveDir) * 5f;
                    }
                    return playerTransform.position;
                }

            // ── Clyde: chase when far, scatter to corner when close ─────────
            case GhostType.Clyde:
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    if (dist > 8f)
                        return playerTransform.position;      // Chase
                    else
                        return scatterTarget;                 // Retreat to patrol corner
                }

            default:
                return playerTransform.position;
        }
    }

    private static readonly Vector3[] CardinalDirs =
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right
    };

    private Vector3 DecideNextDirection(Vector3 target)
    {
        Vector3 bestDir = moveDir;
        float shortestDistance = float.MaxValue;

        Vector3[] dirs = (Vector3[])CardinalDirs.Clone();
        for (int i = 0; i < dirs.Length; i++)
        {
            int rnd = Random.Range(i, dirs.Length);
            Vector3 temp = dirs[rnd]; dirs[rnd] = dirs[i]; dirs[i] = temp;
        }

        bool foundValid = false;
        float cs = (manager != null && manager.mazeData != null ? manager.mazeData.cellSize : 1f);

        foreach (Vector3 dir in dirs)
        {
            if (dir == -moveDir && currentState != GhostState.Frightened) continue;

            Vector3 checkNodePos = transform.position + dir * cs;
            if (!IsWallAtWorldPos(checkNodePos, dir))
            {
                float dist = Vector3.Distance(checkNodePos, target);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    bestDir = dir;
                    foundValid = true;
                }
            }
        }

        if (!foundValid) return -moveDir; // U-turn only if trapped
        return bestDir;
    }

    private Vector3 GetSafeRandomDirection(Vector3 currentDir)
    {
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 tmp = dirs[i]; dirs[i] = dirs[j]; dirs[j] = tmp;
        }

        foreach (var d in dirs)
        {
            if (d == -currentDir) continue;
            if (!IsWallInDirection(d))
                return d;
        }
        return -currentDir;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (currentState == GhostState.Frightened)
        {
            if (manager != null) manager.OnGhostEaten(this);
            SetState(GhostState.Eaten);
        }
        else if (currentState == GhostState.Chase || currentState == GhostState.Scatter)
        {
            if (manager != null) manager.OnPlayerCaught();
        }
    }

    private bool IsWallAtWorldPos(Vector3 worldPos, Vector3 direction)
    {
        if (mazeGen != null && mazeGen.mazeData != null)
        {
            Vector2Int gridPos = mazeGen.WorldToGrid(worldPos);
            if (mazeGen.mazeData.GetCell(gridPos.x, gridPos.y) == MazeData.TileType.Wall)
                return true;
        }

        Vector3 origin = transform.position + Vector3.up * WALL_RAY_UP_OFFSET;
        float rayDist = 0.5f;

        bool hitWall = false;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDist))
        {
            if (hit.collider.CompareTag(wallTag))
                hitWall = true;
        }

        Debug.DrawRay(origin, direction * rayDist, hitWall ? Color.red : Color.green);

        return hitWall;
    }

    private bool IsWallInGrid(Vector3 worldPos)
    {
        if (mazeGen == null || mazeGen.mazeData == null) return false;

        Vector2Int gridPos = mazeGen.WorldToGrid(worldPos);
        return mazeGen.mazeData.GetCell(gridPos.x, gridPos.y) == MazeData.TileType.Wall;
    }

    private bool IsWallInDirection(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * WALL_RAY_UP_OFFSET;
        float rayDist = 0.5f;

        bool hitWall = false;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDist))
        {
            if (hit.collider.CompareTag(wallTag))
                hitWall = true;
        }

        Debug.DrawRay(origin, direction * rayDist, hitWall ? Color.red : Color.green);

        return hitWall;
    }

    private void UpdateColor(Color c)
    {
        if (ghostRenderer != null)
            ghostRenderer.material.color = c;
    }

    private Vector3 SnapToAxis(Vector3 v)
    {
        v.y = 0;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.z))
            return new Vector3(Mathf.Sign(v.x), 0, 0);
        else
            return new Vector3(0, 0, Mathf.Sign(v.z));
    }

    private Color GetClassicColor(GhostType type)
    {
        switch (type)
        {
            case GhostType.Blinky: return Color.red;
            case GhostType.Pinky: return new Color(1f, 0.73f, 0.83f); // Pinkish
            case GhostType.Inky: return Color.cyan;
            case GhostType.Clyde: return new Color(1f, 0.53f, 0f);    // Orange
            default: return Color.white;
        }
    }

    private bool IsColorBlackOrClear(Color c)
    {
        return c.a < 0.1f || (c.r < 0.1f && c.g < 0.1f && c.b < 0.1f);
    }

}

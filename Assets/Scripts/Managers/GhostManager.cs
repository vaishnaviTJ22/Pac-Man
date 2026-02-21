using System.Collections;
using UnityEngine;
public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance { get; private set; }

    [Header("Player Reference")]
    public Transform player;

    [Header("Ghost References (drag from scene)")]
    public GhostController blinky;
    public GhostController pinky;
    public GhostController inky;
    public GhostController clyde;

    [Header("Maze Layout")]
    public MazeData mazeData;

    [Header("Ghost House Positions")]
    public Transform ghostHouseCenter;
    public Transform ghostHouseExit;

    [Header("Scatter Corners (assign transforms or leave for auto)")]
    public Transform blinkyCorner;
    public Transform pinkyCorner;
    public Transform inkyCorner;
    public Transform clydeCorner;

    [Header("Timing")]
    public float releaseInterval = 5f;
    public float chaseModeDuration = 20f;
    public float scatterModeDuration = 7f;

    public GhostController[] ghosts;
    public bool _isChaseMode = true;
    public bool IsChaseMode => _isChaseMode;

    private int ghostsEatenThisEnergizer = 0;
    private static readonly int[] EatScores = { 200, 400, 800, 1600 };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ghosts = new[] { blinky, pinky, inky, clyde };
        foreach (var g in ghosts)
        {
            if (g == null) continue;
            g.playerTransform = player;
            g.houseCenter = ghostHouseCenter != null ? ghostHouseCenter.position : Vector3.zero;
            g.houseExitPoint = ghostHouseExit != null ? ghostHouseExit.position : Vector3.up * 2f;
            g.manager = this;
        }

        if (blinky && blinkyCorner) blinky.scatterTarget = blinkyCorner.position;
        if (pinky && pinkyCorner) pinky.scatterTarget = pinkyCorner.position;
        if (inky && inkyCorner) inky.scatterTarget = inkyCorner.position;
        if (clyde && clydeCorner) clyde.scatterTarget = clydeCorner.position;

        StartCoroutine(ReleaseGhostsRoutine());
        StartCoroutine(ChaseScatterCycleRoutine());
    }
    private IEnumerator ReleaseGhostsRoutine()
    {
        yield return new WaitForSeconds(1f);
        blinky?.Release();

        yield return new WaitForSeconds(releaseInterval);
        pinky?.Release();

        yield return new WaitForSeconds(releaseInterval);
        inky?.Release();

        yield return new WaitForSeconds(releaseInterval);
        clyde?.Release();
    }

    private IEnumerator ChaseScatterCycleRoutine()
    {
        Debug.Log("chase scatter");
        while (true)
        {
            Debug.Log("chase scatter while");
            _isChaseMode = true;
            foreach (var g in ghosts) {

                if (g != null)
                {
                    Debug.Log("Ghosts");
                    g.SetChaseMode(true);
                }
            }
             
            yield return new WaitForSeconds(chaseModeDuration);

           /* _isChaseMode = false;
            foreach (var g in ghosts) g?.SetChaseMode(false);
            yield return new WaitForSeconds(scatterModeDuration);*/
        }
    }

    public void OnEnergizerEaten()
    {
        ghostsEatenThisEnergizer = 0;
        foreach (var g in ghosts) g?.EnterFrightenedMode();
        Debug.Log("[GhostManager] Energizer — ghosts frightened!");
    }

    public void OnGhostEaten(GhostController ghost)
    {
        int score = EatScores[Mathf.Clamp(ghostsEatenThisEnergizer, 0, EatScores.Length - 1)];
        ghostsEatenThisEnergizer++;
        Debug.Log($"[GhostManager] {ghost.ghostType} eaten! +{score}");
        if (GameManager.Instance != null) GameManager.Instance.AddScore(score);
    }

    public void OnPlayerCaught()
    {
        Debug.Log("[GhostManager] Pac-Man caught!");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.OnCaughtByGhost();
        }
    }

    public void ScheduleRespawn(GhostController ghost)
    {
        StartCoroutine(RespawnRoutine(ghost));
    }

    private IEnumerator RespawnRoutine(GhostController ghost)
    {
        yield return new WaitForSeconds(releaseInterval);
        ghost.Release();
    }
    public void ResetAllGhosts()
    {
        StopAllCoroutines();
        foreach (var g in ghosts)
        {
            if (g != null) g.ResetToHouse();
        }
        StartCoroutine(ReleaseGhostsRoutine());
        StartCoroutine(ChaseScatterCycleRoutine());
    }
}

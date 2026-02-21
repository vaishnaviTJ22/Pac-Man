using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns Booster Bottles and Health Orbs at random floor positions.
/// Attach to an empty GameObject.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject bottlePrefab;
    public GameObject orbPrefab;

    [Header("Spawn Settings")]
    public int bottleCount = 2;
    public int orbCount = 2;
    public float spawnHeightOffset = 0.5f;
    public float initialDelay = 5f;
    public float spawnInterval = 10f;
    public float overlapCheckRadius = 0.4f;

    private MazeGenerator mazeGen;

    void Start()
    {
        mazeGen = FindFirstObjectByType<MazeGenerator>();
        if (mazeGen != null)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        if (mazeGen == null || mazeGen.mazeData == null) yield break;

        // Collect all potential tiles that the Grid considers "Empty"
        List<Vector3> potentialPositions = GetPotentialGridPositions();
        
        // Shuffle to randomize placement
        Shuffle(potentialPositions);

        int spawnedBottles = 0;
        int spawnedOrbs = 0;

        foreach (Vector3 pos in potentialPositions)
        {
            // PHYSICAL CHECK: Is the tile ACTUALLY empty of Walls, Dots, etc?
            if (IsTileTrulyEmpty(pos))
            {
                if (spawnedBottles < bottleCount)
                {
                    Instantiate(bottlePrefab, pos + Vector3.up * spawnHeightOffset, Quaternion.identity);
                    spawnedBottles++;
                    
                    float currentInterval = spawnInterval;
                    if (GameManager.Instance != null)
                        currentInterval *= GameManager.Instance.GetIntervalMultiplier();

                    yield return new WaitForSeconds(currentInterval);
                }
                else if (spawnedOrbs < orbCount)
                {
                    Instantiate(orbPrefab, pos + Vector3.up * spawnHeightOffset, Quaternion.identity);
                    spawnedOrbs++;
                    
                    float currentInterval = spawnInterval;
                    if (GameManager.Instance != null)
                        currentInterval *= GameManager.Instance.GetIntervalMultiplier();
                        
                    yield return new WaitForSeconds(currentInterval);
                }
            }

            // Stop if we've spawned everything we need
            if (spawnedBottles >= bottleCount && spawnedOrbs >= orbCount)
                break;
        }
    }

    private bool IsTileTrulyEmpty(Vector3 position)
    {
        // Check for any colliders within the tile radius
        Collider[] colliders = Physics.OverlapSphere(position, overlapCheckRadius);
        
        foreach (var col in colliders)
        {
            // If we hit a wall, a dot, or an energizer, this tile is NOT empty
            if (col.CompareTag("Wall") || col.CompareTag("Dot") || col.CompareTag("Energizer"))
            {
                return false;
            }
        }
        
        return true;
    }

    private List<Vector3> GetPotentialGridPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        MazeData data = mazeGen.mazeData;

        for (int row = 0; row < data.height; row++)
        {
            for (int col = 0; col < data.width; col++)
            {
                MazeData.TileType tile = data.GetCell(col, row);
                // First pass: Only look at tiles metadata says are empty
                if (tile == MazeData.TileType.Empty)
                {
                    positions.Add(mazeGen.GridToWorld(col, row));
                }
            }
        }
        return positions;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}

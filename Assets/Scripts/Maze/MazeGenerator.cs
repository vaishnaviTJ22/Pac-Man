using System.Collections.Generic;
using UnityEngine;
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Layout")]
    public MazeData mazeData;

    [Header("Prefabs (leave empty to auto-create primitives)")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject dotPrefab;
    public GameObject energizerPrefab;

    [Header("Materials (used when auto-creating primitives)")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material dotMaterial;
    public Material energizerMaterial;

    [Header("References (auto-assigned on Generate)")]
    public Transform playerStartMarker;
    public Transform ghostStartMarker;

    private Transform mazeRoot;
    private const string ROOT_NAME = "GeneratedMaze";

    public void Generate()
    {
        if (mazeData == null) { Debug.LogError("[MazeGenerator] No MazeData assigned!"); return; }
        mazeData.EnsureInitialized();

        ClearGenerated();

        mazeRoot = new GameObject(ROOT_NAME).transform;
        mazeRoot.SetParent(transform);
        mazeRoot.localPosition = Vector3.zero;

        float cs = mazeData.cellSize;
        int dotCount = 0;

        for (int row = 0; row < mazeData.height; row++)
        for (int col = 0; col < mazeData.width;  col++)
        {
            Vector3 pos = GridToWorld(col, row);
            MazeData.TileType tile = mazeData.GetCell(col, row);

            switch (tile)
            {
                case MazeData.TileType.Wall:
                    SpawnWall(pos, cs);
                    break;

                case MazeData.TileType.Dot:
                    SpawnFloor(pos, cs);
                    SpawnDot(pos, cs);
                    dotCount++;
                    break;

                case MazeData.TileType.Energizer:
                    SpawnFloor(pos, cs);
                    SpawnEnergizer(pos, cs);
                    dotCount++;
                    break;

                case MazeData.TileType.PlayerStart:
                    SpawnFloor(pos, cs);
                    SetOrCreateMarker(ref playerStartMarker, pos, "PlayerStart");
                    break;

                case MazeData.TileType.GhostStart:
                    SpawnFloor(pos, cs);
                    SetOrCreateMarker(ref ghostStartMarker, pos, "GhostStart");
                    break;

                case MazeData.TileType.GhostHouse:
                case MazeData.TileType.Empty:
                    SpawnFloor(pos, cs);
                    break;
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.totalDots = dotCount;
        }

        Debug.Log($"[MazeGenerator] Generated {mazeData.width}×{mazeData.height} maze with {dotCount} dots.");
    }

    public void ClearGenerated()
    {
        Transform existing = transform.Find(ROOT_NAME);
        if (existing != null)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }
    }

    // ── Spawn Helpers ─────────────────────────────────────────────────────────

    private void SpawnWall(Vector3 pos, float cs)
    {
        GameObject go = wallPrefab
            ? Instantiate(wallPrefab, pos, Quaternion.identity, mazeRoot)
            : CreatePrimitive(PrimitiveType.Cube, pos, new Vector3(cs, cs, cs), wallMaterial, "Wall");

        go.transform.SetParent(mazeRoot);
        go.tag = "Wall";
    }

    private void SpawnFloor(Vector3 pos, float cs)
    {
        GameObject go = floorPrefab
            ? Instantiate(floorPrefab, pos + Vector3.down * cs * 0.5f, Quaternion.identity, mazeRoot)
            : CreatePrimitive(PrimitiveType.Cube,
                              pos + Vector3.down * (cs * 0.5f),
                              new Vector3(cs, 0.1f, cs), floorMaterial, "Floor");
        go.transform.SetParent(mazeRoot);
        // Floors should not block anything
        if (go.TryGetComponent<Collider>(out var col)) col.enabled = false;
    }

    private void SpawnDot(Vector3 pos, float cs)
    {
        if (dotPrefab) { Instantiate(dotPrefab, pos, Quaternion.identity, mazeRoot); return; }

        GameObject go = CreatePrimitive(PrimitiveType.Sphere,
                                        pos, Vector3.one * cs * 0.18f,
                                        dotMaterial, "Dot");
        go.tag = "Dot";
        var col = go.GetComponent<Collider>();
        col.isTrigger = true;
        go.AddComponent<DotController>();
        go.transform.SetParent(mazeRoot);
    }

    private void SpawnEnergizer(Vector3 pos, float cs)
    {
        if (energizerPrefab) { Instantiate(energizerPrefab, pos, Quaternion.identity, mazeRoot); return; }

        GameObject go = CreatePrimitive(PrimitiveType.Sphere,
                                        pos, Vector3.one * cs * 0.38f,
                                        energizerMaterial, "Energizer");
        var col = go.GetComponent<Collider>();
        col.isTrigger = true;
        go.AddComponent<EnergizerController>();
        go.transform.SetParent(mazeRoot);
    }

    private void SetOrCreateMarker(ref Transform marker, Vector3 pos, string name)
    {
        if (marker == null)
        {
            marker = new GameObject(name).transform;
            marker.SetParent(mazeRoot);
        }
        marker.position = pos;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    public Vector3 GridToWorld(int col, int row)
    {
        if (mazeData == null) return transform.position;
        float cs = mazeData.cellSize;
        float startX = -(mazeData.width  - 1) * cs * 0.5f;
        float startZ = -(mazeData.height - 1) * cs * 0.5f;
        return transform.position + new Vector3(startX + col * cs, 0f, startZ + row * cs);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (mazeData == null) return Vector2Int.zero;

        float cs = mazeData.cellSize;
        Vector3 localPos = worldPos - transform.position;
        float startX = -(mazeData.width - 1) * cs * 0.5f;
        float startZ = -(mazeData.height - 1) * cs * 0.5f;

        int col = Mathf.RoundToInt((localPos.x - startX) / cs);
        int row = Mathf.RoundToInt((localPos.z - startZ) / cs);

        return new Vector2Int(col, row);
    }

    private GameObject CreatePrimitive(PrimitiveType type, Vector3 pos,
                                        Vector3 scale, Material mat, string goName)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = goName;
        go.transform.position   = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}

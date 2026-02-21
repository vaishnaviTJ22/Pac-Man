using UnityEngine;
[CreateAssetMenu(fileName = "MazeData", menuName = "PacMan/Maze Data")]
public class MazeData : ScriptableObject
{
    public enum TileType
    {
        Empty       = 0,   // Open floor
        Wall        = 1,   // Solid wall cube
        Dot         = 2,   // Normal pellet
        Energizer   = 3,   // Power pellet (makes ghosts frightened)
        GhostHouse  = 4,   // Interior of ghost house (no walls)
        PlayerStart = 5,   // Pac-Man spawn point
        GhostStart  = 6,   // Ghost house center
    }

    [Header("Grid Size")]
    public int width  = 21;
    public int height = 21;

    [Header("Cell size in Unity units")]
    public float cellSize = 1f;
    [SerializeField] private int[] cells;

    // ── API ───────────────────────────────────────────────────────────────────

    public TileType GetCell(int col, int row)
    {
        if (!InBounds(col, row)) return TileType.Wall;
        return (TileType)cells[row * width + col];
    }

    public void SetCell(int col, int row, TileType type)
    {
        if (!InBounds(col, row)) return;
        cells[row * width + col] = (int)type;
    }

    public bool InBounds(int col, int row)
        => col >= 0 && col < width && row >= 0 && row < height;
    public void Resize(int newWidth, int newHeight)
    {
        int[] old = cells;
        int   oldW = width, oldH = height;

        width  = newWidth;
        height = newHeight;
        cells  = new int[width * height];
        for (int r = 0; r < height; r++)
        for (int c = 0; c < width;  c++)
        {
            bool edge = r == 0 || r == height - 1 || c == 0 || c == width - 1;
            cells[r * width + c] = edge ? (int)TileType.Wall : (int)TileType.Empty;
        }

        if (old != null)
            for (int r = 0; r < Mathf.Min(oldH, height); r++)
            for (int c = 0; c < Mathf.Min(oldW, width);  c++)
                cells[r * width + c] = old[r * oldW + c];
    }

    public void Clear()
    {
        cells = new int[width * height];
        for (int r = 0; r < height; r++)
        for (int c = 0; c < width;  c++)
        {
            bool edge = r == 0 || r == height - 1 || c == 0 || c == width - 1;
            cells[r * width + c] = edge ? (int)TileType.Wall : (int)TileType.Empty;
        }
    }

    public void EnsureInitialized()
    {
        if (cells == null || cells.Length != width * height)
            Clear();
    }
}

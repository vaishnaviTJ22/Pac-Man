using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor for MazeGenerator.
/// Adds a full grid painter to the Inspector with:
///   • Tool palette (Wall / Empty / Dot / Energizer / GhostHouse / PlayerStart / GhostStart)
///   • Click-to-paint cells on the grid
///   • Click-and-drag painting
///   • Generate and Clear buttons
///   • Resize controls
/// </summary>
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    // ── Tool Palette ──────────────────────────────────────────────────────────
    private MazeData.TileType activeTool = MazeData.TileType.Wall;

    private static readonly Color[] TileColors = new Color[]
    {
        new Color(0.15f, 0.15f, 0.15f),  // Empty      — dark grey
        new Color(0.25f, 0.45f, 1.00f),  // Wall       — blue
        new Color(1.00f, 1.00f, 0.80f),  // Dot        — pale yellow
        new Color(1.00f, 0.85f, 0.10f),  // Energizer  — gold
        new Color(0.40f, 0.10f, 0.50f),  // GhostHouse — purple
        new Color(1.00f, 0.85f, 0.10f),  // PlayerStart— yellow
        new Color(1.00f, 0.40f, 0.40f),  // GhostStart — red
    };

    private static readonly string[] TileLabels =
        { "Empty", "Wall", "Dot", "Energizer", "Ghost\nHouse", "Player\nStart", "Ghost\nStart" };

    // Grid painter state
    private bool isPainting = false;
    private const float CELL_PX = 18f;   // pixel size of each cell in Inspector grid
    private const float MAX_GRID_PX = 500f;

    // Resize helpers
    private int pendingW = -1, pendingH = -1;

    // ── Inspector GUI ─────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        MazeGenerator gen = (MazeGenerator)target;
        serializedObject.Update();

        // ── Default fields ────────────────────────────────────────────────────
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("─── Maze Painter ───────────────────────", EditorStyles.boldLabel);

        if (gen.mazeData == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a MazeData asset above.\n" +
                "Create one: right-click in Project → Create → PacMan → Maze Data",
                MessageType.Warning);
            return;
        }

        gen.mazeData.EnsureInitialized();

        // ── Resize ────────────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

        if (pendingW < 0) pendingW = gen.mazeData.width;
        if (pendingH < 0) pendingH = gen.mazeData.height;

        EditorGUILayout.BeginHorizontal();
        pendingW = EditorGUILayout.IntField("Width",  pendingW);
        pendingH = EditorGUILayout.IntField("Height", pendingH);

        if (GUILayout.Button("Resize", GUILayout.Width(60)))
        {
            Undo.RecordObject(gen.mazeData, "Resize Maze");
            gen.mazeData.Resize(Mathf.Clamp(pendingW, 3, 60), Mathf.Clamp(pendingH, 3, 60));
            EditorUtility.SetDirty(gen.mazeData);
        }
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Clear Maze", "Reset all cells?", "Yes", "No"))
            {
                Undo.RecordObject(gen.mazeData, "Clear Maze");
                gen.mazeData.Clear();
                EditorUtility.SetDirty(gen.mazeData);
            }
        }
        EditorGUILayout.EndHorizontal();

        // ── Tool Palette ──────────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Paint Tool", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < TileLabels.Length; i++)
        {
            MazeData.TileType t = (MazeData.TileType)i;
            bool selected = activeTool == t;

            // Draw colored button background
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = selected ? TileColors[i] * 1.6f : TileColors[i];
            GUIStyle style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

            if (GUILayout.Button(TileLabels[i],
                GUILayout.Width(65), GUILayout.Height(36)))
                activeTool = t;

            GUI.backgroundColor = prev;
        }
        EditorGUILayout.EndHorizontal();

        // ── Legend ────────────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        DrawLegend();

        // ── Grid Painter ──────────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Grid  ({gen.mazeData.width} × {gen.mazeData.height})  — click / drag to paint",
                                   EditorStyles.miniLabel);

        DrawGrid(gen);

        // ── Generate / Clear ──────────────────────────────────────────────────
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.4f, 1.0f, 0.4f);
        if (GUILayout.Button("▶  Generate Maze", GUILayout.Height(32)))
        {
            Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Generate Maze");
            gen.Generate();
        }

        GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f);
        if (GUILayout.Button("✕  Clear Scene", GUILayout.Height(32)))
        {
            gen.ClearGenerated();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    // ── Grid Drawing ──────────────────────────────────────────────────────────

    private void DrawGrid(MazeGenerator gen)
    {
        MazeData data = gen.mazeData;
        int w = data.width;
        int h = data.height;

        // Clamp cell size so the grid fits
        float cellPx = Mathf.Min(CELL_PX, MAX_GRID_PX / Mathf.Max(w, h));

        // Reserve rectangle in Inspector
        Rect gridRect = GUILayoutUtility.GetRect(
            w * cellPx,
            h * cellPx,
            GUILayout.ExpandWidth(false));

        // Draw cells
        if (Event.current.type == EventType.Repaint)
        {
            for (int row = 0; row < h; row++)
            for (int col = 0; col < w; col++)
            {
                MazeData.TileType tile = data.GetCell(col, row);
                Rect cellRect = CellRect(gridRect, col, row, h, cellPx);
                EditorGUI.DrawRect(cellRect, TileColors[(int)tile]);

                // Grid line
                Color lineCol = new Color(0f, 0f, 0f, 0.25f);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), lineCol);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), lineCol);
            }
        }

        // Handle mouse paint
        Event e = Event.current;
        bool inGrid = gridRect.Contains(e.mousePosition);

        if (inGrid && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            && e.button == 0)
        {
            int col = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / cellPx);
            int row = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / cellPx);
            row = (h - 1) - row;   // Flip Y so row 0 = bottom

            if (data.InBounds(col, row) && data.GetCell(col, row) != activeTool)
            {
                Undo.RecordObject(data, "Paint Cell");
                data.SetCell(col, row, activeTool);
                EditorUtility.SetDirty(data);
                isPainting = true;
            }
            e.Use();
        }

        if (e.type == EventType.MouseUp) isPainting = false;

        // Repaint inspector continuously while painting
        if (inGrid || isPainting)
            Repaint();
    }

    private Rect CellRect(Rect grid, int col, int row, int totalRows, float cellPx)
    {
        int drawRow = (totalRows - 1) - row;  // flip so row 0 = bottom
        return new Rect(
            grid.x + col      * cellPx,
            grid.y + drawRow  * cellPx,
            cellPx - 1,
            cellPx - 1);
    }

    // ── Legend ────────────────────────────────────────────────────────────────
    private void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        string[] shortLabels = { "Empty", "Wall", "Dot", "Energizer", "GhostHouse", "PlayerStart", "GhostStart" };
        foreach (int i in new[] { 0, 1, 2, 3, 4, 5, 6 })
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = TileColors[i];
            GUILayout.Label(shortLabels[i],
                EditorStyles.miniLabel,
                GUILayout.Width(68));
            GUI.backgroundColor = prev;
        }
        EditorGUILayout.EndHorizontal();
    }
}

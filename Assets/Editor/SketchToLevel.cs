// SketchToLevel.cs
// Umieść w folderze: Assets/Editor/
// Otwórz: Tools -> Sketch To Level
//
// Wczytuje PNG/JPG (czarne kreski = ściany, białe tło = pusto),
// rasteryzuje do siatki i generuje poziom z Unity Cube'ów.
// Nie wymaga żadnych assetów ani zmian w import settings (czyta plik bezpośrednio z dysku).

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SketchToLevel : EditorWindow
{
    // --- stan ---
    string imagePath = "";
    Texture2D sourceTex;
    Texture2D previewTex;
    bool[,] grid;          // true = ściana
    int gridW, gridH;

    // --- parametry ---
    int pixelsPerCell = 4;        // ile pikseli obrazka przypada na 1 komórkę siatki
    float darkThreshold = 0.5f;   // luminancja poniżej = piksel "czarny"
    float coverage = 0.10f;       // min. udział czarnych pikseli w komórce, by uznać ją za ścianę
    float cellSize = 1f;          // metry na komórkę
    float wallHeight = 3f;
    bool generateFloor = true;
    bool mergeBoxes = true;       // scalanie sąsiednich komórek w większe boxy (greedy meshing)

    [MenuItem("Tools/Sketch To Level")]
    static void Open()
    {
        var w = GetWindow<SketchToLevel>("Sketch To Level");
        w.minSize = new Vector2(360, 520);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("1. Obraz (widok z góry, czarne kreski = ściany)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField(imagePath);
        if (GUILayout.Button("Wybierz...", GUILayout.Width(90)))
        {
            string p = EditorUtility.OpenFilePanel("Wybierz szkic poziomu", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(p)) { imagePath = p; LoadImage(); }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("2. Rasteryzacja", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        pixelsPerCell = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Pikseli na komórkę", "Dopasuj do grubości kresek. Kreska ~4 px -> wartość 4 da ścianę o szerokości 1 komórki."), pixelsPerCell));
        darkThreshold = EditorGUILayout.Slider(new GUIContent("Próg czerni", "Luminancja poniżej progu = piksel ściany."), darkThreshold, 0f, 1f);
        coverage = EditorGUILayout.Slider(new GUIContent("Min. pokrycie komórki", "Ile % pikseli komórki musi być czarne. Cienkie kreski -> niska wartość."), coverage, 0.01f, 1f);
        if (EditorGUI.EndChangeCheck() && sourceTex != null) BuildGrid();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("3. Geometria", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Rozmiar komórki [m]", cellSize);
        wallHeight = EditorGUILayout.FloatField("Wysokość ścian [m]", wallHeight);
        generateFloor = EditorGUILayout.Toggle("Generuj podłogę", generateFloor);
        mergeBoxes = EditorGUILayout.Toggle(new GUIContent("Scalaj bloki", "Greedy meshing - dużo mniej GameObjectów."), mergeBoxes);

        EditorGUILayout.Space(6);
        if (previewTex != null)
        {
            EditorGUILayout.LabelField($"Podgląd siatki: {gridW} x {gridH} komórek", EditorStyles.boldLabel);
            float maxW = position.width - 20f;
            float scale = Mathf.Min(maxW / gridW, 260f / gridH);
            Rect r = GUILayoutUtility.GetRect(gridW * scale, gridH * scale);
            EditorGUI.DrawRect(r, Color.gray);
            GUI.DrawTexture(r, previewTex, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.Space(8);
        GUI.enabled = grid != null;
        if (GUILayout.Button("GENERUJ POZIOM", GUILayout.Height(34))) Generate();
        GUI.enabled = true;
    }

    // ---------------------------------------------------------------
    void LoadImage()
    {
        byte[] bytes = File.ReadAllBytes(imagePath);
        sourceTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sourceTex.LoadImage(bytes))
        {
            EditorUtility.DisplayDialog("Błąd", "Nie udało się wczytać obrazka.", "OK");
            sourceTex = null;
            return;
        }
        BuildGrid();
    }

    void BuildGrid()
    {
        gridW = Mathf.Max(1, sourceTex.width / pixelsPerCell);
        gridH = Mathf.Max(1, sourceTex.height / pixelsPerCell);
        grid = new bool[gridW, gridH];

        Color32[] px = sourceTex.GetPixels32();
        int texW = sourceTex.width;
        int texH = sourceTex.height;

        for (int gy = 0; gy < gridH; gy++)
        {
            for (int gx = 0; gx < gridW; gx++)
            {
                int dark = 0, total = 0;
                int x0 = gx * pixelsPerCell;
                int y0 = gy * pixelsPerCell;
                int x1 = Mathf.Min(x0 + pixelsPerCell, texW);
                int y1 = Mathf.Min(y0 + pixelsPerCell, texH);

                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        Color32 c = px[y * texW + x];
                        total++;
                        if (c.a < 128) continue; // przezroczyste = tło
                        float lum = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
                        if (lum < darkThreshold) dark++;
                    }
                }
                grid[gx, gy] = total > 0 && (float)dark / total >= coverage;
            }
        }
        BuildPreview();
    }

    void BuildPreview()
    {
        previewTex = new Texture2D(gridW, gridH, TextureFormat.RGBA32, false);
        previewTex.filterMode = FilterMode.Point;
        var colors = new Color32[gridW * gridH];
        for (int y = 0; y < gridH; y++)
            for (int x = 0; x < gridW; x++)
                colors[y * gridW + x] = grid[x, y] ? new Color32(20, 20, 20, 255) : new Color32(240, 240, 240, 255);
        previewTex.SetPixels32(colors);
        previewTex.Apply();
        Repaint();
    }

    // ---------------------------------------------------------------
    void Generate()
    {
        string rootName = "Level_" + Path.GetFileNameWithoutExtension(imagePath);
        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Level");

        List<RectInt> rects = mergeBoxes ? GreedyMerge() : SingleCells();

        foreach (var rc in rects)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Wall_{rc.x}_{rc.y}_{rc.width}x{rc.height}";
            cube.transform.SetParent(root.transform);
            cube.transform.localScale = new Vector3(rc.width * cellSize, wallHeight, rc.height * cellSize);
            cube.transform.position = new Vector3(
                (rc.x + rc.width * 0.5f) * cellSize,
                wallHeight * 0.5f,
                (rc.y + rc.height * 0.5f) * cellSize);
        }

        if (generateFloor)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(gridW * cellSize, 0.1f, gridH * cellSize);
            floor.transform.position = new Vector3(gridW * cellSize * 0.5f, -0.05f, gridH * cellSize * 0.5f);
        }

        Debug.Log($"[SketchToLevel] Wygenerowano {rects.Count} bloków ścian ({gridW}x{gridH} komórek).");
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    List<RectInt> SingleCells()
    {
        var list = new List<RectInt>();
        for (int y = 0; y < gridH; y++)
            for (int x = 0; x < gridW; x++)
                if (grid[x, y]) list.Add(new RectInt(x, y, 1, 1));
        return list;
    }

    // Greedy meshing: scala sąsiednie komórki w maksymalne prostokąty.
    List<RectInt> GreedyMerge()
    {
        var list = new List<RectInt>();
        var used = new bool[gridW, gridH];

        for (int y = 0; y < gridH; y++)
        {
            for (int x = 0; x < gridW; x++)
            {
                if (!grid[x, y] || used[x, y]) continue;

                // rozszerz w prawo
                int w = 1;
                while (x + w < gridW && grid[x + w, y] && !used[x + w, y]) w++;

                // rozszerz w dół, o ile cały wiersz o szerokości w jest pełny
                int h = 1;
                bool canGrow = true;
                while (canGrow && y + h < gridH)
                {
                    for (int i = 0; i < w; i++)
                    {
                        if (!grid[x + i, y + h] || used[x + i, y + h]) { canGrow = false; break; }
                    }
                    if (canGrow) h++;
                }

                for (int yy = 0; yy < h; yy++)
                    for (int xx = 0; xx < w; xx++)
                        used[x + xx, y + yy] = true;

                list.Add(new RectInt(x, y, w, h));
            }
        }
        return list;
    }
}

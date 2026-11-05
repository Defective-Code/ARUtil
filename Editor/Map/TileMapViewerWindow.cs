using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-time viewer for the .otil archive. Pan/zoom to frame the map, then push the
/// view centre and zoom into a TileMapPanel's start fields.
/// </summary>
public class TileMapViewerWindow : EditorWindow
{
    TileMapPanel panel;
    string fileName = "map.otil";           // used when no panel is assigned

    TileArchive archive;
    string loadedPath;
    DateTime loadedStamp;
    string loadError;

    double cu = 0.5, cv = 0.5;              // view centre, normalised Mercator
    float zoom = 1f;
    double uMin, uMax, vMin, vMax;

    bool showFrame = true;
    int frameW = 540, frameH = 960;         // runtime viewport size, in panel units

    readonly Dictionary<ulong, Texture2D> cache = new Dictionary<ulong, Texture2D>();
    Vector2 hoverMouse;

    [MenuItem("Tools/Tile Map Viewer")]
    public static void OpenWindow() => Open(Selection.activeGameObject != null
        ? Selection.activeGameObject.GetComponent<TileMapPanel>() : null);

    public static void Open(TileMapPanel p)
    {
        var w = GetWindow<TileMapViewerWindow>("Tile Map Viewer");
        if (p != null) { w.panel = p; w.loadedPath = null; }
        w.wantsMouseMove = true;
        w.Show();
    }

    void OnEnable() { wantsMouseMove = true; }
    void OnDisable() { DisposeAll(); }

    // ------------------------------------------------------------ archive

    float TileUnits => panel != null ? panel.displayTileSize : 256f;
    float OverZoom => panel != null ? panel.overZoom : 1f;

    void LoadArchive()
    {
        string name = panel != null ? panel.archiveFileName : fileName;
        string path = Path.Combine(Application.streamingAssetsPath, name ?? "");
        DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        if (path == loadedPath && stamp == loadedStamp) return;

        DisposeAll();
        loadedPath = path;
        loadedStamp = stamp;
        loadError = null;
        if (stamp == DateTime.MinValue) { loadError = "Archive not found: " + path; return; }

        try { archive = TileArchive.Open(path); }
        catch (Exception e) { loadError = e.Message; return; }

        uMin = TileMapPanel.LonToU(archive.MinLon);
        uMax = TileMapPanel.LonToU(archive.MaxLon);
        vMin = TileMapPanel.LatToV(archive.MaxLat);
        vMax = TileMapPanel.LatToV(archive.MinLat);
        GoToStart();
    }

    void DisposeAll()
    {
        foreach (var t in cache.Values) if (t != null) DestroyImmediate(t);
        cache.Clear();
        archive?.Dispose();
        archive = null;
    }

    Texture2D GetTile(int z, int x, int y)
    {
        ulong key = TileArchive.Key(z, x, y);
        if (cache.TryGetValue(key, out Texture2D t)) return t;

        Texture2D tex = null;
        if (archive.TryGetTile(z, x, y, out byte[] data))
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!tex.LoadImage(data)) { DestroyImmediate(tex); tex = null; }
        }
        cache[key] = tex;
        return tex;
    }

    // ------------------------------------------------------------ view maths

    double World(float z) => TileUnits * Math.Pow(2.0, z);

    float ClampZoom(float z) => Mathf.Clamp(z, archive.MinZoom - 0.5f, archive.MaxZoom + OverZoom);

    void ClampCentre()
    {
        cu = Math.Max(uMin, Math.Min(uMax, cu));
        cv = Math.Max(vMin, Math.Min(vMax, cv));
    }

    static void UVToLatLon(double u, double v, out double lat, out double lon)
    {
        lon = u * 360.0 - 180.0;
        lat = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * v))) * 180.0 / Math.PI;
    }

    void GoToStart()
    {
        if (archive == null) return;
        float mid = (archive.MinZoom + archive.MaxZoom) * 0.5f;
        cu = (uMin + uMax) * 0.5;
        cv = (vMin + vMax) * 0.5;
        zoom = mid;

        if (panel != null && panel.startPosition == TileMapPanel.StartPositionMode.Coordinate)
        {
            cu = TileMapPanel.LonToU(panel.startLongitude);
            cv = TileMapPanel.LatToV(panel.startLatitude);
        }
        if (panel != null && panel.startZoom > 0f) zoom = panel.startZoom;

        zoom = ClampZoom(zoom);
        ClampCentre();
    }

    void ZoomAbout(float newZoom, Vector2 focus, Vector2 viewSize)
    {
        double dx = focus.x - viewSize.x * 0.5, dy = focus.y - viewSize.y * 0.5;
        double w0 = World(zoom);
        double fu = cu + dx / w0, fv = cv + dy / w0;
        zoom = ClampZoom(newZoom);
        double w1 = World(zoom);
        cu = fu - dx / w1;
        cv = fv - dy / w1;
        ClampCentre();
    }

    // ------------------------------------------------------------ GUI

    void OnGUI()
    {
        panel = (TileMapPanel)EditorGUILayout.ObjectField("Map panel", panel, typeof(TileMapPanel), true);
        if (panel == null) fileName = EditorGUILayout.TextField("Archive file name", fileName);

        if (Event.current.type == EventType.Layout) LoadArchive();

        if (archive == null)
        {
            EditorGUILayout.HelpBox(loadError ?? "No archive loaded.", MessageType.Warning);
            return;
        }

        // Zoom slider (same range the runtime allows).
        EditorGUI.BeginChangeCheck();
        float z = EditorGUILayout.Slider("Zoom", zoom, archive.MinZoom - 0.5f, archive.MaxZoom + OverZoom);
        if (EditorGUI.EndChangeCheck()) { zoom = ClampZoom(z); }

        using (new EditorGUILayout.HorizontalScope())
        {
            showFrame = EditorGUILayout.ToggleLeft("Show runtime frame", showFrame, GUILayout.Width(150));
            using (new EditorGUI.DisabledScope(!showFrame))
            {
                EditorGUIUtility.labelWidth = 14;
                frameW = Mathf.Max(1, EditorGUILayout.IntField("W", frameW));
                frameH = Mathf.Max(1, EditorGUILayout.IntField("H", frameH));
                EditorGUIUtility.labelWidth = 0;
            }
        }

        Rect view = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        HandleInput(view);
        if (Event.current.type == EventType.Repaint) DrawMap(view);

        DrawFooter();
    }

    void HandleInput(Rect view)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive, view);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (view.Contains(e.mousePosition) && (e.button == 0 || e.button == 2))
                { GUIUtility.hotControl = id; e.Use(); }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    double w = World(zoom);
                    cu -= e.delta.x / w;
                    cv -= e.delta.y / w;
                    ClampCentre();
                    e.Use(); Repaint();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
                break;

            case EventType.ScrollWheel:
                if (view.Contains(e.mousePosition))
                {
                    float dir = -Mathf.Sign(e.delta.y);
                    ZoomAbout(zoom + dir * 0.25f, e.mousePosition - view.position, view.size);
                    e.Use(); Repaint();
                }
                break;

            case EventType.MouseMove:
                hoverMouse = e.mousePosition - view.position;
                Repaint();
                break;
        }
    }

    void DrawMap(Rect view)
    {
        Vector2 vp = view.size;
        double world = World(zoom);
        int z = Mathf.Clamp(Mathf.RoundToInt(zoom), archive.MinZoom, archive.MaxZoom);
        int n = 1 << z;
        double tileScreen = world / n;

        if (cache.Count > 400)
        {
            foreach (var t in cache.Values) if (t != null) DestroyImmediate(t);
            cache.Clear();
        }

        GUI.BeginClip(view);
        EditorGUI.DrawRect(new Rect(0, 0, vp.x, vp.y), new Color(0.95f, 0.94f, 0.91f));

        double halfW = vp.x * 0.5 / world, halfH = vp.y * 0.5 / world;
        int tx0 = Mathf.Clamp((int)Math.Floor((cu - halfW) * n), 0, n - 1);
        int tx1 = Mathf.Clamp((int)Math.Floor((cu + halfW) * n), 0, n - 1);
        int ty0 = Mathf.Clamp((int)Math.Floor((cv - halfH) * n), 0, n - 1);
        int ty1 = Mathf.Clamp((int)Math.Floor((cv + halfH) * n), 0, n - 1);

        for (int ty = ty0; ty <= ty1; ty++)
            for (int tx = tx0; tx <= tx1; tx++)
            {
                Texture2D tex = GetTile(z, tx, ty);
                if (tex == null) continue;

                float l = (float)(vp.x * 0.5 + (tx / (double)n - cu) * world);
                float t = (float)(vp.y * 0.5 + (ty / (double)n - cv) * world);
                float x0 = Mathf.Round(l), y0 = Mathf.Round(t);
                float x1 = Mathf.Round(l + (float)tileScreen), y1 = Mathf.Round(t + (float)tileScreen);
                GUI.DrawTexture(new Rect(x0, y0, x1 - x0, y1 - y0), tex, ScaleMode.StretchToFill, false);
            }

        Vector2 c = vp * 0.5f;
        if (showFrame)
        {
            var dim = new Color(0, 0, 0, 0.45f);
            float fl = c.x - frameW * 0.5f, ft = c.y - frameH * 0.5f;
            float fr = fl + frameW, fb = ft + frameH;
            EditorGUI.DrawRect(new Rect(0, 0, vp.x, Mathf.Max(0, ft)), dim);
            EditorGUI.DrawRect(new Rect(0, Mathf.Min(vp.y, fb), vp.x, Mathf.Max(0, vp.y - fb)), dim);
            EditorGUI.DrawRect(new Rect(0, Mathf.Max(0, ft), Mathf.Max(0, fl), Mathf.Min(frameH, vp.y)), dim);
            EditorGUI.DrawRect(new Rect(Mathf.Min(vp.x, fr), Mathf.Max(0, ft), Mathf.Max(0, vp.x - fr), Mathf.Min(frameH, vp.y)), dim);

            var line = new Color(1f, 1f, 1f, 0.9f);
            EditorGUI.DrawRect(new Rect(fl, ft, frameW, 1), line);
            EditorGUI.DrawRect(new Rect(fl, fb - 1, frameW, 1), line);
            EditorGUI.DrawRect(new Rect(fl, ft, 1, frameH), line);
            EditorGUI.DrawRect(new Rect(fr - 1, ft, 1, frameH), line);
        }

        // Crosshair = the start position that will be captured.
        var cross = new Color(1f, 0.2f, 0.1f);
        EditorGUI.DrawRect(new Rect(c.x - 10, c.y - 0.5f, 20, 1), cross);
        EditorGUI.DrawRect(new Rect(c.x - 0.5f, c.y - 10, 1, 20), cross);
        GUI.EndClip();
    }

    void DrawFooter()
    {
        UVToLatLon(cu, cv, out double lat, out double lon);

        Rect v = GUILayoutUtility.GetLastRect();
        double world = World(zoom);
        double hu = cu + (hoverMouse.x - (position.width * 0.5)) / world; // approximate; see below
        EditorGUILayout.LabelField(
            string.Format(CultureInfo.InvariantCulture,
                "Centre: {0:F6}, {1:F6}   Zoom: {2:F2}", lat, lon, zoom),
            EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset to panel's start view")) GoToStart();

            if (GUILayout.Button("Copy 'lat, lon, zoom'"))
                GUIUtility.systemCopyBuffer = string.Format(CultureInfo.InvariantCulture,
                    "{0:F6}, {1:F6}, {2:F2}", lat, lon, zoom);

            using (new EditorGUI.DisabledScope(panel == null))
            {
                if (GUILayout.Button("Apply to panel"))
                    ApplyToPanel(lat, lon);
            }
        }
    }

    void ApplyToPanel(double lat, double lon)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("startPosition").enumValueIndex = (int)TileMapPanel.StartPositionMode.Coordinate;
        so.FindProperty("startLatitude").doubleValue = Math.Round(lat, 6);
        so.FindProperty("startLongitude").doubleValue = Math.Round(lon, 6);
        so.FindProperty("startZoom").floatValue = (float)Math.Round(zoom, 2);
        so.ApplyModifiedProperties();       // handles undo, dirtying and prefab overrides
    }
}
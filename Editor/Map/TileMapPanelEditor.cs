using System.Globalization;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileMapPanel), true)]
public class TileMapPanelEditor : Editor
{

    TileArchive.Info info;
    bool hasInfo;
    string infoPath;
    DateTime infoStamp;


    void RefreshInfo(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName ?? "");
        DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        if (path == infoPath && stamp == infoStamp) return;   // cached

        infoPath = path;
        infoStamp = stamp;
        hasInfo = stamp != DateTime.MinValue && TileArchive.TryReadInfo(path, out info);
    }

    public override void OnInspectorGUI()
    {
        var map = (TileMapPanel)target;
        serializedObject.Update();
        RefreshInfo(serializedObject.FindProperty("archiveFileName").stringValue);

        var it = serializedObject.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(it);
            }
            else if (it.propertyPath == "startZoom")
            {
                DrawZoomSlider(it, serializedObject.FindProperty("overZoom").floatValue);
            }
            else
            {
                EditorGUILayout.PropertyField(it, true);
            }
        }
        serializedObject.ApplyModifiedProperties();

        DrawArchiveInfo(serializedObject);
        DrawStartViewTools(map);
    }

    void DrawZoomSlider(SerializedProperty zoom, float overZoom)
    {
        if (!hasInfo)
        {
            // No readable archive: fall back to a plain field.
            EditorGUILayout.PropertyField(zoom);
            EditorGUILayout.HelpBox("Archive not found or unreadable in StreamingAssets, so the zoom range is unknown.", MessageType.Warning);
            return;
        }

        float min = info.MinZoom;
        float max = info.MaxZoom + overZoom;
        float mid = (info.MinZoom + info.MaxZoom) * 0.5f;

        bool auto = zoom.floatValue <= 0f;
        EditorGUI.BeginChangeCheck();
        bool newAuto = EditorGUILayout.Toggle(new GUIContent("Start Zoom: Auto", "Middle of the archive's zoom range"), auto);
        if (EditorGUI.EndChangeCheck())
            zoom.floatValue = newAuto ? 0f : mid;

        if (!newAuto)
        {
            float v = Mathf.Clamp(zoom.floatValue, min, max);
            zoom.floatValue = EditorGUILayout.Slider(new GUIContent("Start Zoom"), v, min, max);
        }
    }

    void DrawArchiveInfo(SerializedObject so)
    {
        if (!hasInfo) return;
        EditorGUILayout.HelpBox(
            $"Archive zoom levels {info.MinZoom}-{info.MaxZoom} ({info.TileCount} tiles, {info.TileSize}px).\n" +
            $"Coverage: lat {info.MinLat:F4} to {info.MaxLat:F4}, lon {info.MinLon:F4} to {info.MaxLon:F4}.",
            MessageType.None);

        var mode = so.FindProperty("startPosition");
        if (mode.enumValueIndex == (int)TileMapPanel.StartPositionMode.Coordinate)
        {
            double lat = so.FindProperty("startLatitude").doubleValue;
            double lon = so.FindProperty("startLongitude").doubleValue;
            if (lat < info.MinLat || lat > info.MaxLat || lon < info.MinLon || lon > info.MaxLon)
                EditorGUILayout.HelpBox("Start coordinate is outside the archive's coverage and will be clamped to its edge.", MessageType.Warning);
        }
    }

    void DrawStartViewTools(TileMapPanel map)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Start view tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Open Map Viewer"))
            TileMapViewerWindow.Open(map);

        using (new EditorGUI.DisabledScope(!Application.isPlaying || !map.IsReady))
        {
            if (GUILayout.Button("Preview start view (play mode)"))
                map.ApplyStartView();

            if (GUILayout.Button("Copy current view to clipboard (play mode)"))
            {
                map.GetCentreLatLon(out double lat, out double lon);
                GUIUtility.systemCopyBuffer = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F6}, {1:F6}, {2:F2}", lat, lon, map.ZoomLevel);
            }
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Paste view from clipboard → Start fields"))
                PasteView(map);
        }

        EditorGUILayout.HelpBox(
            "Workflow: enter play mode, pan/zoom to the framing you want, click 'Copy current view', " +
            "stop play mode, click 'Paste view'. Format: lat, lon, zoom.", MessageType.Info);
    }
    void PasteView(TileMapPanel map)
    {
        string[] p = GUIUtility.systemCopyBuffer.Split(',');
        if (p.Length != 3 ||
            !double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
            !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) ||
            !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float zoom))
        {
            Debug.LogWarning("Clipboard doesn't contain 'lat, lon, zoom'.");
            return;
        }

        Undo.RecordObject(map, "Paste start view");
        map.startPosition = TileMapPanel.StartPositionMode.Coordinate;
        map.startLatitude = lat;
        map.startLongitude = lon;
        map.startZoom = zoom;
        EditorUtility.SetDirty(map);
    }
}
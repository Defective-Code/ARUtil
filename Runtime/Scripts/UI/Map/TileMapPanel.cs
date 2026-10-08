using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Offline tile map for UI Toolkit, using the new Input System.
///
/// Streams standard XYZ (slippy-map) tiles from a single .otil archive (see pack_tiles.py),
/// so the map stays sharp at every zoom level. Tiles are standard Web Mercator, so lat/lon
/// positions are exact - there is no manual georeferencing step.
///
/// Input:
///  - Drag to pan (UI Toolkit pointer events, fed by the Input System UI Input Module), with inertia
///  - Mouse wheel to zoom about the cursor (UI Toolkit WheelEvent)
///  - Pinch to zoom on touch screens (Input System EnhancedTouch)
///
/// Setup:
///  - Put the .otil archive in Assets/StreamingAssets and name it in "Archive File Name"
///  - A UIDocument with a PanelSettings asset, managed by your UIManager as usual
///  - An EventSystem with an "Input System UI Input Module"
///  - Project Settings > Player > Active Input Handling: "Input System Package (New)" or "Both"
///  - Unity 2021.2+ (UI Toolkit transform styles: translate / rotate)
///
/// The player marker follows LocationData (your ScriptableObject): this class subscribes to
/// locationDataUpdated and reads latitude / longitude. It does no GPS polling of its own.
///
/// The UXML may contain a VisualElement named "map-viewport" (with a size) to embed the map in
/// a larger layout; otherwise one that fills the root is created. An optional Button named
/// "map-close-button" pops the view.
/// </summary>
public class TileMapPanel : UIViewBehaviour
{
    [Header("Tile archive")]
    [Tooltip("File in Assets/StreamingAssets produced by pack_tiles.py")]
    public string archiveFileName = "map.otil";

    [Header("Layout")]
    public string viewportName = "map-viewport";
    public Color viewportBackground = new Color(0.95f, 0.94f, 0.91f);

    [Header("Zoom")]
    [Tooltip("Panel units one tile covers at its native zoom level. Keep 256 even with 512 px tiles for crisp high-DPI screens")]
    public float displayTileSize = 256f;
    [Tooltip("Initial zoom level. 0 or less = middle of the archive's zoom range")]
    public float startZoom = 0f;
    [Tooltip("Zoom levels past the archive's last level the map may magnify (tiles get soft)")]
    public float overZoom = 1f;
    [Tooltip("Zoom levels per mouse-wheel notch")]
    public float wheelZoomStep = 0.5f;
    public bool invertWheel = false;

    [Header("Tile loading")]
    public int maxTileLoadsPerFrame = 6;
    [Tooltip("Decoded tiles kept in memory (visible tiles are never evicted)")]
    public int textureCacheMB = 96;
    [Tooltip("Tiles are drawn this many units larger to hide hairline seams when scaled")]
    public float seamOverlap = 0f;

    [Header("Panning")]
    [Tooltip("Pointer must move this far (panel units) before a press becomes a drag; keeps marker taps working")]
    public float dragThreshold = 8f;
    [Tooltip("Higher = inertia stops sooner")]
    public float inertiaDecay = 6f;

    [Header("Markers (tint colour is applied to icons)")]
    public Texture2D keyPointIcon;          // leave empty for a coloured circle
    public Color keyPointColor = new Color(1f, 0.55f, 0f);
    public Vector2 keyPointSize = new Vector2(28, 28);
    public Texture2D playerIcon;            // should point "up" if heading is used
    public Color playerColor = new Color(0.1f, 0.45f, 1f);
    public Vector2 playerSize = new Vector2(32, 32);

    [Header("GPS")]
    [Tooltip("Rotate the player marker using the bearing between GPS fixes")]
    public bool headingFromMovement = true;
    public float minMoveForHeadingMetres = 3f;
    [SerializeField] public LocationData locationData;

    /// <summary>Fired with the marker id when a key point is tapped/clicked.</summary>
    public event Action<string> MarkerClicked;
    /// <summary>Fired once the tile archive has been opened.</summary>
    public event Action Ready;

    public bool IsReady => archive != null;
    public float ZoomLevel => zoomF;

    // ---- UI elements ----
    VisualElement viewport;
    VisualElement tileLayer;
    VisualElement playerElement;
    bool viewportCreated;

    // ---- archive & view state ----
    TileArchive archive;
    Coroutine openRoutine;
    // View centre in normalised world coordinates: u = 0..1 west->east, v = 0..1 north->south.
    double cu = 0.5, cv = 0.5;
    float zoomF = 1f;                       // continuous zoom level
    double uMin, uMax, vMin, vMax;          // coverage of the archive, normalised
    bool viewSetByUser;
    bool dirty;

    // ---- tiles ----
    class Tile
    {
        public int z, x, y;
        public ulong key;
        public VisualElement element;
        public bool removed;
        public double dist;
    }
    readonly Dictionary<ulong, Tile> tiles = new Dictionary<ulong, Tile>();
    readonly List<Tile> pending = new List<Tile>();
    readonly List<ulong> removeBuffer = new List<ulong>();
    int shownLevel = -1;

    // ---- texture cache (LRU) ----
    class CacheEntry { public ulong key; public Texture2D tex; public long bytes; }
    readonly Dictionary<ulong, LinkedListNode<CacheEntry>> cache = new Dictionary<ulong, LinkedListNode<CacheEntry>>();
    readonly LinkedList<CacheEntry> lru = new LinkedList<CacheEntry>();
    long cacheBytes;

    // ---- drag state ----
    int pressedId = -1;
    bool dragging;
    Vector2 downPos, lastPos, dragAccum, velocity;
    bool pinching;

    // ---- data ----
    class MarkerData
    {
        public string id;
        public double lat, lon, u, v;
        public Texture2D icon;
        public Color color;
        public VisualElement element;
    }
    readonly Dictionary<string, MarkerData> markers = new Dictionary<string, MarkerData>();
    bool hasPlayer;
    double playerU, playerV;
    float playerHeading;

    // ---- gps ----
    bool hasPrevFix;
    double prevFixLat, prevFixLon;

    protected override void OnInitialize()
    {
        EnhancedTouchSupport.Enable();
        BindUI(Root);

        // Open the tile archive (asynchronous on Android, where it has to be copied out of the APK first).
        openRoutine = StartCoroutine(OpenArchive());

        //GPS subscription
        if (locationData != null) locationData.locationDataUpdated += OnGpsFix;
    }

    private void BindUI(VisualElement root)
    {
        // Check if the viewport field is null or empty, if it is then we want to create a new VisualElement and add it to the uxml root, otherwise we just search for it within the root.
        viewport = string.IsNullOrEmpty(viewportName) ? null : root.Q<VisualElement>(viewportName);
        viewportCreated = viewport == null;
        if (viewportCreated)
        {
            viewport = new VisualElement { name = viewportName };
            var vs = viewport.style;
            vs.position = Position.Absolute;
            vs.left = 0; vs.top = 0; vs.right = 0; vs.bottom = 0;
            root.Add(viewport);
        }

        viewport.style.overflow = Overflow.Hidden;
        viewport.style.backgroundColor = viewportBackground;
        viewport.pickingMode = PickingMode.Position;

        // Tiles live in their own layer underneath the markers.
        tileLayer = new VisualElement { name = "tile-layer", pickingMode = PickingMode.Ignore };
        var ts = tileLayer.style;
        ts.position = Position.Absolute;
        ts.left = 0; ts.top = 0; ts.right = 0; ts.bottom = 0;
        viewport.Add(tileLayer);

        viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
        viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
        viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        viewport.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        viewport.RegisterCallback<WheelEvent>(OnWheel);
        viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        Button mapCloseButton = root.Q<Button>("map-close-button");
        if (mapCloseButton != null)
        {
            mapCloseButton.clicked += () =>
            {
                UIManager.Instance.Pop();
            };
        }

        // Key points added before binding, then the player marker (hidden until we have a position).
        foreach (MarkerData m in markers.Values)
        {
            m.element = CreateMarkerElement(m.id, m.icon, m.color, keyPointSize, true);
            viewport.Add(m.element);
        }
        playerElement = CreateMarkerElement("player", playerIcon, playerColor, playerSize, false);
        playerElement.style.display = DisplayStyle.None;
        viewport.Add(playerElement);
        ApplyPlayerHeading();

        shownLevel = -1;
        dirty = true;
    }

    void TearDown()
    {
        if (viewport == null) return;

        viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        viewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        viewport.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        viewport.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        viewport.UnregisterCallback<WheelEvent>(OnWheel);
        viewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        tileLayer?.RemoveFromHierarchy();
        foreach (MarkerData m in markers.Values) { m.element?.RemoveFromHierarchy(); m.element = null; }
        playerElement?.RemoveFromHierarchy();
        if (viewportCreated) viewport.RemoveFromHierarchy();

        tiles.Clear();
        pending.Clear();
        viewport = null;
        tileLayer = null;
        playerElement = null;
        pressedId = -1;
        dragging = false;
    }

    void OnDestroy()
    {
        if (locationData != null) locationData.locationDataUpdated -= OnGpsFix;
        EnhancedTouchSupport.Disable();
        TearDown();

        foreach (CacheEntry e in lru) if (e.tex != null) Destroy(e.tex);
        lru.Clear();
        cache.Clear();
        cacheBytes = 0;

        archive?.Dispose();
        archive = null;
    }

    // ======================================================== archive access

    IEnumerator OpenArchive()
    {
        string path = null;
        yield return EnsureLocalFile(archiveFileName, p => path = p);
        if (path == null) { openRoutine = null; yield break; }

        try
        {
            archive = TileArchive.Open(path);
        }
        catch (Exception e)
        {
            Debug.LogError("TileMapPanel: could not open tile archive: " + e.Message);
            openRoutine = null;
            yield break;
        }

        uMin = LonToU(archive.MinLon);
        uMax = LonToU(archive.MaxLon);
        vMin = LatToV(archive.MaxLat);      // north edge = smaller v
        vMax = LatToV(archive.MinLat);

        if (!viewSetByUser)
        {
            cu = (uMin + uMax) * 0.5;
            cv = (vMin + vMax) * 0.5;
            zoomF = startZoom > 0f ? startZoom : (archive.MinZoom + archive.MaxZoom) * 0.5f;
        }
        zoomF = ClampZoom(zoomF);
        ClampCentre();

        dirty = true;
        openRoutine = null;
        Ready?.Invoke();
    }

    /// <summary>
    /// Returns a real file path for a StreamingAssets file. On Android, StreamingAssets lives
    /// inside the APK and can't be opened with FileStream, so it's copied once to
    /// persistentDataPath (and again whenever the app build changes).
    /// </summary>
    IEnumerator EnsureLocalFile(string fileName, Action<string> done)
    {
        string src = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        string dst = Path.Combine(Application.persistentDataPath, fileName);
        string stampFile = dst + ".stamp";
        string stamp = Application.version + "|" + Application.buildGUID;

        bool upToDate = File.Exists(dst) && File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp;
        if (!upToDate)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(src))
            {
                // Streams straight to disk, so a large archive never sits in memory.
                req.downloadHandler = new DownloadHandlerFile(dst) { removeFileOnAbort = true };
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("TileMapPanel: failed to copy archive from StreamingAssets: " + req.error);
                    done(null);
                    yield break;
                }
            }
            File.WriteAllText(stampFile, stamp);
        }
        done(dst);
#else
        if (!File.Exists(src))
        {
            Debug.LogError("TileMapPanel: archive not found at " + src);
            done(null);
            yield break;
        }
        done(src);
        yield break;
#endif
    }

    // ========================================================== public API

    /// <summary>Add (or replace) a key point. Hidden automatically if outside the archive's coverage.</summary>
    public void AddMarker(string id, double lat, double lon, Texture2D icon = null, Color? color = null)
    {
        RemoveMarker(id);
        var m = new MarkerData
        {
            id = id,
            lat = lat,
            lon = lon,
            u = LonToU(lon),
            v = LatToV(lat),
            icon = icon != null ? icon : keyPointIcon,
            color = color ?? keyPointColor
        };
        markers[id] = m;

        if (viewport != null)
        {
            m.element = CreateMarkerElement(m.id, m.icon, m.color, keyPointSize, true);
            viewport.Add(m.element);
            playerElement.BringToFront();       // player always on top
            dirty = true;
        }
    }

    public void RemoveMarker(string id)
    {
        if (markers.TryGetValue(id, out MarkerData m))
        {
            m.element?.RemoveFromHierarchy();
            markers.Remove(id);
        }
    }

    public void ClearMarkers()
    {
        foreach (MarkerData m in markers.Values) m.element?.RemoveFromHierarchy();
        markers.Clear();
    }

    /// <summary>Move the player marker. Also called automatically from LocationData updates.</summary>
    public void SetPlayerPosition(double lat, double lon)
    {
        hasPlayer = true;
        playerU = LonToU(lon);
        playerV = LatToV(lat);
        dirty = true;
    }

    /// <summary>Rotate the player marker; degrees clockwise from north (map is north-up).</summary>
    public void SetPlayerHeading(float degrees)
    {
        playerHeading = degrees;
        ApplyPlayerHeading();
    }

    public void CentreOnPlayer()
    {
        if (!hasPlayer) return;
        velocity = Vector2.zero;
        viewSetByUser = true;
        cu = playerU;
        cv = playerV;
        if (archive != null) ClampCentre();
        dirty = true;
    }

    /// <summary>Centre the map on a coordinate, optionally setting the zoom level (e.g. 16).</summary>
    public void CentreOn(double lat, double lon, float zoom = -1f)
    {
        velocity = Vector2.zero;
        viewSetByUser = true;
        cu = LonToU(lon);
        cv = LatToV(lat);
        if (zoom > 0f) zoomF = zoom;
        if (archive != null) { zoomF = ClampZoom(zoomF); ClampCentre(); }
        dirty = true;
    }

    /// <summary>
    /// Set the zoom level (e.g. 15.5), zooming about a point in viewport coordinates
    /// (default: the viewport centre).
    /// </summary>
    public void SetZoom(float zoomLevel, Vector2? focus = null)
    {
        if (archive == null || viewport == null) return;

        Vector2 vp = ViewportSize();
        Vector2 f = focus ?? vp * 0.5f;
        double dx = f.x - vp.x * 0.5;
        double dy = f.y - vp.y * 0.5;

        // World point under the focus stays put while the zoom changes.
        double w0 = WorldSize(zoomF);
        double fu = cu + dx / w0;
        double fv = cv + dy / w0;

        zoomF = ClampZoom(zoomLevel);

        double w1 = WorldSize(zoomF);
        cu = fu - dx / w1;
        cv = fv - dy / w1;
        ClampCentre();
        dirty = true;
    }

    // ===================================================== lat/lon conversion

    // Normalised Web Mercator: u = 0..1 west->east, v = 0..1 north->south (same as XYZ tiles).
    public static double LonToU(double lon)
    {
        return (lon + 180.0) / 360.0;
    }

    public static double LatToV(double lat)
    {
        lat = Math.Max(-85.0511, Math.Min(85.0511, lat));
        double r = lat * Math.PI / 180.0;
        return (1.0 - Math.Log(Math.Tan(r) + 1.0 / Math.Cos(r)) / Math.PI) / 2.0;
    }

    double WorldSize(float zoom)
    {
        return displayTileSize * Math.Pow(2.0, zoom);
    }

    float ClampZoom(float z)
    {
        return Mathf.Clamp(z, archive.MinZoom - 0.5f, archive.MaxZoom + overZoom);
    }

    void ClampCentre()
    {
        cu = Math.Max(uMin, Math.Min(uMax, cu));
        cv = Math.Max(vMin, Math.Min(vMax, cv));
    }

    bool Covered(double u, double v)
    {
        return u >= uMin && u <= uMax && v >= vMin && v <= vMax;
    }

    // ============================================================== markers

    VisualElement CreateMarkerElement(string id, Texture2D icon, Color color, Vector2 size, bool clickable)
    {
        var el = new VisualElement { name = "marker_" + id };
        el.pickingMode = clickable ? PickingMode.Position : PickingMode.Ignore;   // don't block map drags

        var s = el.style;
        s.position = Position.Absolute;
        s.width = size.x;
        s.height = size.y;
        // Centre the element on its left/top anchor point.
        s.translate = new StyleTranslate(new Translate(Length.Percent(-50), Length.Percent(-50)));

        if (icon != null)
        {
            s.backgroundImage = new StyleBackground(icon);
            s.unityBackgroundImageTintColor = color;
        }
        else
        {
            // Plain coloured circle with a white outline.
            s.backgroundColor = color;
            float r = Mathf.Min(size.x, size.y) * 0.5f;
            s.borderTopLeftRadius = r; s.borderTopRightRadius = r;
            s.borderBottomLeftRadius = r; s.borderBottomRightRadius = r;
            s.borderTopWidth = 2; s.borderRightWidth = 2; s.borderBottomWidth = 2; s.borderLeftWidth = 2;
            s.borderTopColor = Color.white; s.borderRightColor = Color.white;
            s.borderBottomColor = Color.white; s.borderLeftColor = Color.white;
        }

        if (clickable)
        {
            string captured = id;
            el.RegisterCallback<ClickEvent>(evt =>
            {
                MarkerClicked?.Invoke(captured);
                evt.StopPropagation();
            });
        }
        return el;
    }

    void RefreshMarkers(Vector2 vp, double world)
    {
        foreach (MarkerData m in markers.Values)
            if (m.element != null) PlaceElement(m.element, m.u, m.v, vp, world);

        if (hasPlayer && playerElement != null)
            PlaceElement(playerElement, playerU, playerV, vp, world);
    }

    // Markers are positioned straight in viewport coordinates (not inside a scaled parent),
    // so they keep a constant on-screen size without any counter-scaling.
    void PlaceElement(VisualElement el, double u, double v, Vector2 vp, double world)
    {
        if (!Covered(u, v)) { el.style.display = DisplayStyle.None; return; }

        el.style.display = DisplayStyle.Flex;
        el.style.left = (float)(vp.x * 0.5 + (u - cu) * world);
        el.style.top = (float)(vp.y * 0.5 + (v - cv) * world);
    }

    void ApplyPlayerHeading()
    {
        if (playerElement == null) return;
        playerElement.style.rotate = new StyleRotate(
            new Rotate(new Angle(playerHeading, AngleUnit.Degree)));
    }

    // ================================================================ view

    Vector2 ViewportSize()
    {
        Vector2 v = viewport.layout.size;
        return float.IsNaN(v.x) || float.IsNaN(v.y) ? Vector2.zero : v;
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        dirty = true;
    }

    // Lays out the visible tiles and markers for the current centre / zoom. Called from Update
    // when something changed, so many pointer events in one frame cost a single refresh.
    void Refresh()
    {
        if (archive == null || viewport == null) return;

        Vector2 vp = ViewportSize();
        if (vp.x < 1f || vp.y < 1f) return;

        double world = WorldSize(zoomF);
        int z = Mathf.Clamp(Mathf.RoundToInt(zoomF), archive.MinZoom, archive.MaxZoom);

        if (z != shownLevel)
        {
            ClearTiles();
            shownLevel = z;
        }

        int n = 1 << z;
        double tileScreen = world / n;
        double halfW = vp.x * 0.5 / world;      // half the viewport, in normalised world units
        double halfH = vp.y * 0.5 / world;

        int tx0 = Mathf.Clamp((int)Math.Floor((cu - halfW) * n), 0, n - 1);
        int tx1 = Mathf.Clamp((int)Math.Floor((cu + halfW) * n), 0, n - 1);
        int ty0 = Mathf.Clamp((int)Math.Floor((cv - halfH) * n), 0, n - 1);
        int ty1 = Mathf.Clamp((int)Math.Floor((cv + halfH) * n), 0, n - 1);

        float size = (float)tileScreen + seamOverlap;

        // Create any newly-visible tiles (doesn't touch position yet).
        for (int ty = ty0; ty <= ty1; ty++)
            for (int tx = tx0; tx <= tx1; tx++)
            {
                ulong key = TileArchive.Key(z, tx, ty);
                if (!tiles.ContainsKey(key)) CreateTile(z, tx, ty, key);
            }

        // Drop tiles that have scrolled well off screen (keep a one-tile margin so a tile
        // doesn't get destroyed and immediately recreated while straddling the edge).
        removeBuffer.Clear();
        foreach (var kv in tiles)
        {
            Tile t = kv.Value;
            if (t.x < tx0 - 1 || t.x > tx1 + 1 || t.y < ty0 - 1 || t.y > ty1 + 1)
                removeBuffer.Add(kv.Key);
        }
        foreach (ulong k in removeBuffer)
        {
            Tile t = tiles[k];
            t.removed = true;
            t.element.RemoveFromHierarchy();
            tiles.Remove(k);
        }

        // Reposition every tile that's still alive - including the one-tile margin kept
        // around for removal above. Previously this only ran for tx0..tx1/ty0..ty1, so a
        // margin tile's position went stale for a frame (or more, mid-drag) before it was
        // removed, which showed up as a sliver of the old tile stuck to the trailing edge.
        foreach (Tile t in tiles.Values)
        {
            var s = t.element.style;
            s.left = (float)(vp.x * 0.5 + (t.x / (double)n - cu) * world);
            s.top = (float)(vp.y * 0.5 + (t.y / (double)n - cv) * world);
            s.width = size;
            s.height = size;
        }

        // Load nearest-to-centre tiles first.
        if (pending.Count > 1)
        {
            foreach (Tile t in pending)
            {
                double du = (t.x + 0.5) / n - cu, dv = (t.y + 0.5) / n - cv;
                t.dist = du * du + dv * dv;
            }
            pending.Sort((a, b) => b.dist.CompareTo(a.dist));     // nearest at the end
        }

        RefreshMarkers(vp, world);
    }

    // ================================================================ tiles

    Tile CreateTile(int z, int x, int y, ulong key)
    {
        var el = new VisualElement { name = "tile_" + z + "_" + x + "_" + y, pickingMode = PickingMode.Ignore };
        el.style.position = Position.Absolute;
        tileLayer.Add(el);

        var t = new Tile { z = z, x = x, y = y, key = key, element = el };
        tiles[key] = t;

        if (TryGetCached(key, out Texture2D tex))
            el.style.backgroundImage = new StyleBackground(tex);
        else
            pending.Add(t);
        return t;
    }

    void ClearTiles()
    {
        foreach (Tile t in tiles.Values) { t.removed = true; t.element.RemoveFromHierarchy(); }
        tiles.Clear();
        pending.Clear();
    }

    void LoadPendingTiles()
    {
        int budget = maxTileLoadsPerFrame;
        while (budget > 0 && pending.Count > 0)
        {
            Tile t = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (t.removed) continue;
            budget--;

            Texture2D tex = null;
            if (archive.TryGetTile(t.z, t.x, t.y, out byte[] data))
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data, true))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    AddToCache(t.key, tex);
                }
                else
                {
                    Destroy(tex);
                    tex = null;
                }
            }
            // Tiles missing from the archive simply show the background colour.
            if (tex != null) t.element.style.backgroundImage = new StyleBackground(tex);
        }
    }

    bool TryGetCached(ulong key, out Texture2D tex)
    {
        if (cache.TryGetValue(key, out LinkedListNode<CacheEntry> node))
        {
            lru.Remove(node);
            lru.AddFirst(node);
            tex = node.Value.tex;
            return true;
        }
        tex = null;
        return false;
    }

    void AddToCache(ulong key, Texture2D tex)
    {
        var e = new CacheEntry { key = key, tex = tex, bytes = (long)tex.width * tex.height * 4 };
        cache[key] = lru.AddFirst(e);
        cacheBytes += e.bytes;
        TrimCache();
    }

    void TrimCache()
    {
        long limit = (long)textureCacheMB * 1024L * 1024L;
        LinkedListNode<CacheEntry> node = lru.Last;
        while (cacheBytes > limit && node != null)
        {
            LinkedListNode<CacheEntry> prev = node.Previous;
            CacheEntry e = node.Value;
            if (!tiles.ContainsKey(e.key))              // never evict a tile that's on screen
            {
                cacheBytes -= e.bytes;
                cache.Remove(e.key);
                lru.Remove(node);
                if (e.tex != null) Destroy(e.tex);
            }
            node = prev;
        }
    }

    // ===================================================== pointer handling

    void PanByPixels(Vector2 delta)
    {
        double world = WorldSize(zoomF);
        cu -= delta.x / world;
        cv -= delta.y / world;
        ClampCentre();
        dirty = true;
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (pinching || pressedId != -1) return;

        pressedId = evt.pointerId;
        downPos = lastPos = evt.position;
        dragging = false;
        velocity = Vector2.zero;
        dragAccum = Vector2.zero;
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != pressedId || pinching || archive == null) return;

        Vector2 pos = evt.position;

        if (!dragging)
        {
            // Don't capture until the pointer has really moved, so taps still reach markers.
            if ((pos - downPos).magnitude < dragThreshold) return;
            dragging = true;
            viewSetByUser = true;
            viewport.CapturePointer(pressedId);
            lastPos = pos;
            return;
        }

        Vector2 delta = pos - lastPos;
        lastPos = pos;
        dragAccum += delta;
        PanByPixels(delta);
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId == pressedId) EndDrag();
    }

    void OnPointerCancel(PointerCancelEvent evt)
    {
        if (evt.pointerId == pressedId) EndDrag();
    }

    void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId == pressedId) EndDrag();
    }

    void EndDrag()
    {
        if (viewport != null && pressedId >= 0 && viewport.HasPointerCapture(pressedId))
            viewport.ReleasePointer(pressedId);
        pressedId = -1;
        dragging = false;
    }

    void OnWheel(WheelEvent evt)
    {
        if (Mathf.Approximately(evt.delta.y, 0f)) return;

        // Sign only - the magnitude differs between platforms and input modules.
        float dir = -Mathf.Sign(evt.delta.y);
        if (invertWheel) dir = -dir;

        viewSetByUser = true;
        SetZoom(zoomF + dir * wheelZoomStep, evt.localMousePosition);
        evt.StopPropagation();
    }

    // ================================================================ update

    void Update()
    {
        if (viewport == null || viewport.panel == null) return;     // not bound, or detached from the panel

        HandlePinch();
        UpdateInertia();

        if (dirty)
        {
            dirty = false;
            Refresh();
        }
        if (archive != null) LoadPendingTiles();
    }

    // Pinch zoom via the new Input System's EnhancedTouch API.
    void HandlePinch()
    {
        var touches = Touch.activeTouches;
        if (touches.Count < 2 || archive == null) { pinching = false; return; }

        Touch a = touches[0], b = touches[1];
        Vector2 midLocal = ScreenToViewport((a.screenPosition + b.screenPosition) * 0.5f);

        if (!pinching)
        {
            if (!viewport.ContainsPoint(midLocal)) return;     // pinch started outside the map
            pinching = true;
            EndDrag();
            velocity = Vector2.zero;
        }

        float now = Vector2.Distance(a.screenPosition, b.screenPosition);
        float prev = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
        if (prev > 1f && now > 1f)
        {
            viewSetByUser = true;
            SetZoom(zoomF + Mathf.Log(now / prev, 2f), midLocal);   // doubling the distance = +1 zoom level
        }
    }

    Vector2 ScreenToViewport(Vector2 screen)
    {
        if (viewport.panel == null) return Vector2.zero;
        // UI Toolkit screen space has y pointing down.
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
            viewport.panel, new Vector2(screen.x, Screen.height - screen.y));
        return viewport.WorldToLocal(panelPos);
    }

    void UpdateInertia()
    {
        float dt = Time.unscaledDeltaTime;

        if (dragging)
        {
            // Smoothed drag velocity (panel units / second).
            velocity = Vector2.Lerp(velocity, dragAccum / Mathf.Max(dt, 1e-4f), 0.5f);
            dragAccum = Vector2.zero;
        }
        else if (pressedId == -1 && velocity.sqrMagnitude > 4f)
        {
            PanByPixels(velocity * dt);
            velocity *= Mathf.Exp(-inertiaDecay * dt);
        }
        else if (pressedId == -1)
        {
            velocity = Vector2.zero;
        }
    }

    // ================================================================== gps

    void OnGpsFix()
    {
        float lat = locationData.latitude;
        float lon = locationData.longitude;

        SetPlayerPosition(lat, lon);

        if (!headingFromMovement) return;

        if (!hasPrevFix) { hasPrevFix = true; prevFixLat = lat; prevFixLon = lon; return; }

        // Small-distance approximation, plenty accurate for a few metres.
        double dy = (lat - prevFixLat) * 111320.0;
        double dx = (lon - prevFixLon) * 111320.0 * Math.Cos(lat * Math.PI / 180.0);
        if (Math.Sqrt(dx * dx + dy * dy) < minMoveForHeadingMetres) return;   // jitter, not movement

        float bearing = (float)(Math.Atan2(dx, dy) * 180.0 / Math.PI);       // clockwise from north
        SetPlayerHeading((bearing + 360f) % 360f);
        prevFixLat = lat;
        prevFixLon = lon;
    }
}
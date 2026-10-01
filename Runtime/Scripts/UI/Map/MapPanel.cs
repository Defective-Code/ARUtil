using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Offline pannable/zoomable map for UI Toolkit, using the new Input System.
///
/// A pre-rendered Web Mercator (EPSG:3857) map image is shown inside a clipped viewport.
/// Because the image's lat/lon bounds are known, GPS coordinates convert to pixel positions,
/// so key points and the player's position are VisualElements that pan/zoom with the map.
///
/// Input:
///  - Drag to pan (UI Toolkit pointer events, fed by the Input System UI Input Module), with inertia
///  - Mouse wheel to zoom about the cursor (UI Toolkit WheelEvent)
///  - Pinch to zoom on touch screens (Input System EnhancedTouch)
///
/// Scene requirements:
///  - A UIDocument with a PanelSettings asset (this component sits on the same GameObject)
///  - An EventSystem with an "Input System UI Input Module"
///  - Project Settings > Player > Active Input Handling: "Input System Package (New)" or "Both"
///  - Unity 2021.2+ (UI Toolkit transform styles: translate / scale / rotate)
///
/// By default the map fills the whole panel. To embed it in a larger layout, add a
/// VisualElement named "map-viewport" in your UXML (with a size) and it will be used instead.
/// </summary>


public class MapPanel : UIViewBehaviour
{
    [Header("Map image & georeference")]
    public Texture2D mapTexture;
    [Tooltip("Geographic extent of the image (degrees, WGS84)")]
    public double minLat, maxLat, minLon, maxLon;

    [Header("Layout")]
    public string viewportName = "map-viewport";
    public Color viewportBackground = new Color(0.12f, 0.12f, 0.12f);

    [Header("Zoom (scales the whole image)")]
    public float minZoom = 0.5f;
    public float maxZoom = 4f;
    public float wheelZoomStep = 0.15f;
    public bool invertWheel = false;

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
    //public bool useDeviceGps = true;
    [Tooltip("Rotate the player marker using the bearing between GPS fixes")]
    public bool headingFromMovement = true;
    public float minMoveForHeadingMetres = 3f;
    [SerializeField] public LocationData locationData;

    /// <summary>Fired with the marker id when a key point is tapped/clicked.</summary>
    public event Action<string> MarkerClicked;

    // ---- UI elements (recreated on enable) ----
    VisualElement viewport;
    VisualElement content;
    VisualElement playerElement;
    bool viewportCreated;

    // ---- view state ----
    float zoom = 1f;
    Vector2 offset;             // content centre relative to viewport centre (panel units, y down)

    // ---- drag state ----
    int pressedId = -1;
    bool dragging;
    Vector2 downPos, lastPos, dragAccum, velocity;
    bool pinching;

    // ---- data (survives UIDocument rebuilds) ----
    class MarkerData
    {
        public string id;
        public double lat, lon;
        public Texture2D icon;
        public Color color;
        public VisualElement element;
    }
    readonly Dictionary<string, MarkerData> markers = new Dictionary<string, MarkerData>();
    bool hasPlayer;
    double playerLat, playerLon;
    float playerHeading;

    // ---- gps ----
    Coroutine gpsRoutine;
    bool gpsStarted;
    double lastGpsTimestamp;
    bool hasPrevFix;
    double prevFixLat, prevFixLon;

    Vector2 ContentSize => new Vector2(mapTexture.width, mapTexture.height);

    protected override void OnInitialize()
    {
        EnhancedTouchSupport.Enable();
        BindUI(Root);

        //GPS subscription
        locationData.locationDataUpdated += OnGpsFix;
    }

    private void BindUI(VisualElement root)
    {
        if (mapTexture == null)
        {
            Debug.LogError("MapPanel: no map texture assigned.");
            return;
        }

        // Check if the viewport field is null or emtpy, if it is then we want to create a new VisualElement and add it to the uxml root, otherwise we just search for it within the root. 
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

        // The map image: sized to the texture, scaled/translated with transform styles.
        content = new VisualElement { name = "map-content", pickingMode = PickingMode.Ignore };
        var cs = content.style;
        cs.position = Position.Absolute;
        cs.left = 0; cs.top = 0;
        cs.width = mapTexture.width;
        cs.height = mapTexture.height;
        cs.backgroundImage = new StyleBackground(mapTexture);
        cs.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(Length.Percent(50), Length.Percent(50)));
        viewport.Add(content);


        viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
        viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
        viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        viewport.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        viewport.RegisterCallback<WheelEvent>(OnWheel);
        viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        Button mapCloseButton = root.Q<Button>("map-close-button");
        mapCloseButton.clicked += () =>
        {
            UIManager.Instance.Pop();
        };

        // Player marker (hidden until we have a position), then any key points added earlier.
        playerElement = CreateMarkerElement("player", playerIcon, playerColor, playerSize, false);
        content.Add(playerElement);
        playerElement.style.display = DisplayStyle.None;

        foreach (MarkerData m in markers.Values)
        {
            m.element = null;
            CreateAndPlace(m);
        }
        if (hasPlayer)
        {
            PlaceElement(playerElement, playerLat, playerLon);
            ApplyPlayerHeading();
        }
        playerElement.BringToFront();

        UpdateMarkerScales();
        ApplyTransform();


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

        content?.RemoveFromHierarchy();
        if (viewportCreated) viewport.RemoveFromHierarchy();

        foreach (MarkerData m in markers.Values) m.element = null;
        viewport = null;
        content = null;
        playerElement = null;
        pressedId = -1;
        dragging = false;
    }

    // ========================================================== public API

    /// <summary>Add (or replace) a key point. Hidden automatically if outside the map.</summary>
    public void AddMarker(string id, double lat, double lon, Texture2D icon = null, Color? color = null)
    {
        RemoveMarker(id);
        var m = new MarkerData
        {
            id = id,
            lat = lat,
            lon = lon,
            icon = icon != null ? icon : keyPointIcon,
            color = color ?? keyPointColor
        };
        markers[id] = m;

        if (content != null)
        {
            CreateAndPlace(m);
            playerElement.BringToFront();       // player always on top
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

    /// <summary>Move the player marker. Call this yourself if you aren't using device GPS.</summary>
    public void SetPlayerPosition(double lat, double lon)
    {
        hasPlayer = true;
        playerLat = lat;
        playerLon = lon;
        if (playerElement != null) PlaceElement(playerElement, lat, lon);
    }

    /// <summary>Rotate the player marker; degrees clockwise from north (map is north-up).</summary>
    public void SetPlayerHeading(float degrees)
    {
        playerHeading = degrees;
        ApplyPlayerHeading();
    }

    public void CentreOnPlayer()
    {
        if (hasPlayer) CentreOn(playerLat, playerLon);
    }

    public void CentreOn(double lat, double lon)
    {
        if (content == null || !TryLatLonToLocal(lat, lon, out Vector2 p)) return;

        velocity = Vector2.zero;
        offset = -(p - ContentSize * 0.5f) * zoom;
        ClampOffset();
        ApplyTransform();
    }

    /// <summary>Zoom about a point given in viewport-local coordinates (default: viewport centre).</summary>
    public void SetZoom(float newZoom, Vector2? focus = null)
    {
        if (content == null) return;
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

        Vector2 half = ViewportSize() * 0.5f;
        Vector2 f = focus ?? half;
        Vector2 centre = half + offset;
        Vector2 newCentre = f - (f - centre) * (newZoom / zoom);   // keeps the focus point fixed

        zoom = newZoom;
        offset = newCentre - half;
        ClampOffset();
        ApplyTransform();
        UpdateMarkerScales();
    }

    // ===================================================== lat/lon conversion

    /// <summary>
    /// Converts lat/lon to a position in the map image, in pixels from its TOP-left
    /// (UI Toolkit's y axis points down). Returns false if the point is outside the map.
    /// </summary>
    public bool TryLatLonToLocal(double lat, double lon, out Vector2 pos)
    {
        double fx = (lon - minLon) / (maxLon - minLon);
        double fy = (MercY(lat) - MercY(minLat)) / (MercY(maxLat) - MercY(minLat));

        Vector2 size = ContentSize;
        pos = new Vector2((float)(fx * size.x), (float)((1.0 - fy) * size.y));
        return fx >= 0 && fx <= 1 && fy >= 0 && fy <= 1;
    }

    // Web Mercator y (unitless) - the image is linear in this space, NOT in latitude.
    static double MercY(double lat)
    {
        lat = Math.Max(-85.0511, Math.Min(85.0511, lat));
        return Math.Log(Math.Tan(Math.PI / 4.0 + lat * Math.PI / 360.0));
    }

    /// <summary>
    /// Georeferencing helper: converts EPSG:3857 metres (e.g. from a QGIS export extent or
    /// world file) to WGS84 lat/lon degrees.
    /// </summary>
    public static void MercatorMetresToLatLon(double x, double y, out double lat, out double lon)
    {
        const double R = 6378137.0;
        lon = x / R * 180.0 / Math.PI;
        lat = (2.0 * Math.Atan(Math.Exp(y / R)) - Math.PI / 2.0) * 180.0 / Math.PI;
    }


    // ============================================================== markers

    void CreateAndPlace(MarkerData m)
    {
        m.element = CreateMarkerElement(m.id, m.icon, m.color, keyPointSize, true);
        content.Add(m.element);
        PlaceElement(m.element, m.lat, m.lon);
        ApplyMarkerScale(m.element);
    }

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

    void PlaceElement(VisualElement el, double lat, double lon)
    {
        bool inside = TryLatLonToLocal(lat, lon, out Vector2 p);
        el.style.left = p.x;
        el.style.top = p.y;
        el.style.display = inside ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Markers are children of the scaled map, so counter-scale to keep a constant on-screen size.
    void ApplyMarkerScale(VisualElement el)
    {
        float inv = 1f / zoom;
        el.style.scale = new StyleScale(new Scale(new Vector2(inv, inv)));
    }

    void UpdateMarkerScales()
    {
        foreach (MarkerData m in markers.Values)
            if (m.element != null) ApplyMarkerScale(m.element);
        if (playerElement != null) ApplyMarkerScale(playerElement);
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

    void ApplyTransform()
    {
        if (content == null) return;

        Vector2 v = ViewportSize();
        Vector2 c = ContentSize;
        // Content's layout origin is the viewport's top-left; move its centre to viewport centre + offset.
        Vector2 t = v * 0.5f - c * 0.5f + offset;

        content.style.translate = new StyleTranslate(new Translate(t.x, t.y));
        content.style.scale = new StyleScale(new Scale(new Vector2(zoom, zoom)));
    }

    void ClampOffset()
    {
        Vector2 max = Vector2.Max(Vector2.zero, (ContentSize * zoom - ViewportSize()) * 0.5f);
        offset = new Vector2(Mathf.Clamp(offset.x, -max.x, max.x),
                             Mathf.Clamp(offset.y, -max.y, max.y));
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        ClampOffset();
        ApplyTransform();
    }


    // ===================================================== pointer handling

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
        if (evt.pointerId != pressedId || pinching) return;

        Vector2 pos = evt.position;

        if (!dragging)
        {
            // Don't capture until the pointer has really moved, so taps still reach markers.
            if ((pos - downPos).magnitude < dragThreshold) return;
            dragging = true;
            viewport.CapturePointer(pressedId);
            lastPos = pos;
            return;
        }

        Vector2 delta = pos - lastPos;
        lastPos = pos;
        offset += delta;
        dragAccum += delta;
        ClampOffset();
        ApplyTransform();
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

        SetZoom(zoom * (1f + dir * wheelZoomStep), evt.localMousePosition);
        evt.StopPropagation();
    }


    // ================================================================ update

    void Update()
    {
        if (viewport == null) return;

        HandlePinch();
        UpdateInertia();
    }

    // Pinch zoom via the new Input System's EnhancedTouch API.
    void HandlePinch()
    {
        var touches = Touch.activeTouches;
        if (touches.Count < 2) { pinching = false; return; }

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
            SetZoom(zoom * (now / prev), midLocal);
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
            offset += velocity * dt;
            velocity *= Mathf.Exp(-inertiaDecay * dt);
            ClampOffset();
            ApplyTransform();
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

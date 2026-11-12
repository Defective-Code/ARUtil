using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ImageViewer : UIViewBehaviour, IPayloadReceiver
{
    [SerializeField] float referenceDpi = 360f;

    [Header("Feel")]
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float wheelZoomSpeed = 0.05f;
    public float doubleTapTime = 0.3f;
    public float maxDoubleTapDistance = 50f;
    public float doubleTapZoom = 2.5f; // absolute target scale (your old +15 always clamped to max)

    VisualElement overlayRoot, viewport;
    Image imageElement;
    Button closeButton, leftArrow, rightArrow;

    Texture2D[] _images;
    int currentImage;

    // Single source of truth: pointerId -> last known position (panel space). Max 2 tracked.
    readonly Dictionary<int, Vector2> pointers = new();

    Vector2 position;
    float scale = 1f;

    // pinch state
    float pinchStartDistance, pinchStartScale;
    Vector2 lastCentroid;

    // double tap / tap detection
    public float maxTapDuration = 0.25f;   // longer press than this isn't a tap
    public float tapSlop = 15f;            // max finger travel (dpi-scaled) for a tap

    float lastTapTime = -10f;
    Vector2 lastTapPosition;

    // per-gesture state (a gesture = first finger down -> all fingers up)
    bool gestureWasMulti;
    bool gestureMoved;
    bool doubleTapConsumed;
    float gestureStartTime;
    Vector2 gestureStartPos;

    // Guard against instant closing due to the way opening works - raycast on TouchPhase.Began, so rest of touch goes through to this UI element which instantly closes it
    // Add small timer to guard against this instant close
    float openedAt;
    const float closeGuardTime = 0.35f;

    protected override void OnInitialize()
    {
        overlayRoot = Root.Q<VisualElement>("overlay");
        overlayRoot.style.display = DisplayStyle.None;
        viewport = Root.Q<VisualElement>("viewport");
        imageElement = Root.Q<Image>("image");
        closeButton = Root.Q<Button>("closebutton");
        leftArrow = Root.Q<Button>("left-arrow");
        rightArrow = Root.Q<Button>("right-arrow");

        closeButton?.RegisterCallback<ClickEvent>(_ => Close());
        viewport?.RegisterCallback<ClickEvent>(_ => Close());
        imageElement.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

        imageElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        imageElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        imageElement.RegisterCallback<PointerUpEvent>(evt => EndPointer(evt.pointerId));
        imageElement.RegisterCallback<PointerCancelEvent>(evt => EndPointer(evt.pointerId, true));
        imageElement.RegisterCallback<PointerCaptureOutEvent>(evt => EndPointer(evt.pointerId, true));
        imageElement.RegisterCallback<WheelEvent>(OnWheel);

        leftArrow?.RegisterCallback<ClickEvent>(_ => Step(-1));
        rightArrow?.RegisterCallback<ClickEvent>(_ => Step(+1));

        ResetView();
    }

    public void SetPayload(object payload) => _images = payload as Texture2D[];

    public override void OnEnter()
    {
        openedAt = Time.unscaledTime;

        // Checking if slideshow is needed or not
        bool multi = _images != null && _images.Length > 1;
        var display = multi ? DisplayStyle.Flex : DisplayStyle.None;
        if (leftArrow != null) leftArrow.style.display = display;
        if (rightArrow != null) rightArrow.style.display = display;

        if (_images != null && _images.Length > 0)
        {
            currentImage = 0;
            ShowImage(0);
        }
        overlayRoot.style.display = DisplayStyle.Flex;
    }

    public override void OnExit()
    {
        overlayRoot.style.display = DisplayStyle.None;
        imageElement.image = null;
        _images = null;
        pointers.Clear();
    }

    public void Close()
    {
        if (Time.unscaledTime - openedAt < closeGuardTime) return;
        UIManager.Instance.Pop();
    }

    // ---------- image cycling ----------
    void Step(int dir)
    {
        currentImage = (currentImage + dir + _images.Length) % _images.Length;
        ShowImage(currentImage);
    }

    void ShowImage(int index)
    {
        var tex = _images[index];
        imageElement.style.width = Length.Percent(100);
        imageElement.style.height = StyleKeyword.Auto;
        imageElement.style.aspectRatio = (float)tex.width / tex.height;
        imageElement.image = tex;
        ResetView();
    }

    public void ResetView()
    {
        scale = 1f;
        position = Vector2.zero;
        Apply();
    }

    // ---------- input ----------
    void OnPointerDown(PointerDownEvent evt)
    {
        // check if the finger is on the edge of the screen and if it is, ignore it
        //if (CheckTouchPosition.CheckTouchDistanceToBorder(evt.position)) return;

        if (pointers.Count >= 2) return; // ignore 3rd+ finger

        bool startingGesture = pointers.Count == 0;
        pointers[evt.pointerId] = evt.position;
        imageElement.CapturePointer(evt.pointerId);

        if (pointers.Count == 2)
        {
            gestureWasMulti = true; // this gesture can never register as a tap
            BeginPinch();
            return;
        }

        if (!startingGesture) return;

        // first finger of a new gesture
        gestureWasMulti = false;
        gestureMoved = false;
        gestureStartTime = Time.unscaledTime;
        gestureStartPos = evt.position;

        doubleTapConsumed =
            Time.unscaledTime - lastTapTime < doubleTapTime &&
            Vector2.Distance(evt.position, lastTapPosition) < maxDoubleTapDistance * DpiScale();

        if (doubleTapConsumed)
        {
            if (scale > minZoom + 0.01f) ResetView();
            else ZoomTowards(evt.position, Mathf.Clamp(doubleTapZoom, minZoom, maxZoom));
            lastTapTime = -10f;
        }
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!pointers.TryGetValue(evt.pointerId, out Vector2 prev)) return;
        Vector2 cur = evt.position;
        pointers[evt.pointerId] = cur;

        if (pointers.Count == 1)
        {
            if (!gestureMoved &&
                Vector2.Distance(cur, gestureStartPos) > tapSlop * DpiScale())
                gestureMoved = true;

            position += cur - prev;
            Apply();
        }
        else if (pointers.Count == 2)
        {
            GetPair(out Vector2 a, out Vector2 b);
            Vector2 centroid = (a + b) * 0.5f;
            float dist = Vector2.Distance(a, b);

            position += centroid - lastCentroid; // two-finger pan
            lastCentroid = centroid;

            float target = Mathf.Clamp(pinchStartScale * dist / pinchStartDistance, minZoom, maxZoom);
            ZoomTowards(centroid, target); // also calls Apply()
        }
    }

    void EndPointer(int id, bool cancelled = false)
    {
        if (!pointers.Remove(id)) return;
        if (imageElement.HasPointerCapture(id)) imageElement.ReleasePointer(id);

        if (pointers.Count > 0) return; // still mid-gesture (e.g. one finger left after a pinch)

        // All fingers are up: decide whether this whole gesture was a tap.
        bool isTap =
            !cancelled &&
            !gestureWasMulti &&
            !gestureMoved &&
            !doubleTapConsumed &&
            Time.unscaledTime - gestureStartTime < maxTapDuration;

        if (isTap)
        {
            lastTapTime = Time.unscaledTime;
            lastTapPosition = gestureStartPos;
        }
        else
        {
            lastTapTime = -10f; // pinches, drags, long presses and the end of a double-tap all clear the window
        }
    }

    void OnWheel(WheelEvent evt)
    {
        scale = Mathf.Clamp(scale - evt.delta.y * wheelZoomSpeed, minZoom, maxZoom);
        Apply();
    }

    // ---------- pinch helpers ----------
    void BeginPinch()
    {
        GetPair(out Vector2 a, out Vector2 b);
        pinchStartDistance = Mathf.Max(Vector2.Distance(a, b), 1f);
        pinchStartScale = scale;
        lastCentroid = (a + b) * 0.5f;
    }

    void GetPair(out Vector2 a, out Vector2 b)
    {
        a = b = default;
        int i = 0;
        foreach (var p in pointers.Values)
        {
            if (i == 0) a = p; else b = p;
            if (++i == 2) break;
        }
    }

    // ---------- transform ----------
    void ZoomTowards(Vector2 panelPoint, float targetScale)
    {
        Vector2 focal = panelPoint - viewport.worldBound.center;
        float old = scale;
        scale = targetScale;
        position = focal + (position - focal) * (scale / old);
        Apply();
    }

    void Apply()
    {
        ClampPosition();
        imageElement.style.scale = new Scale(new Vector3(scale, scale, 1f));
        imageElement.style.translate = new Translate(position.x, position.y);
    }

    void ClampPosition()
    {
        float w = viewport.resolvedStyle.width, h = viewport.resolvedStyle.height;
        float maxX = Mathf.Max(0, (w * scale - w) * 0.5f);
        float maxY = Mathf.Max(0, (h * scale - h) * 0.5f);
        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        position.y = Mathf.Clamp(position.y, -maxY, maxY);
    }

    float DpiScale()
    {
        float dpi = Screen.dpi;
        return dpi <= 0f ? 1f : dpi / referenceDpi;
    }
}
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

using System;
using System.Collections.Generic;

public class ImageViewer : UIViewBehaviour, IPayloadReceiver
{
    private float referenceDpi = 360f; // dpi of the test device. Basically, once the "Feel" for the test device is correct, this will adjust the pixel values so that that feel auto translates to other dpi's/screen sizes by nomralizing against the dpi of the device used to "tune" the values

    private Texture2D _pending;

    public bool debug;

    // Variables to change for feel
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float pinchSpeed = 0.01f;
    public float wheelZoomSpeed = 0.05f;
    public float doubleTapTime = 0.3f;
    public float minDistanceBetweenTaps = 50f; // max screen distance between successive taps to count as a double-tap (rather than two pinch fingers landing)
    public float defaultZoomValue = 15f; // added to scale (then clamped) when double tapping in

    // UI Elements 
    [Header("Overlay root - the element to show/hide")]
    public VisualElement overlayRoot;
    private Image imageElement;
    private VisualElement viewport;
    private Button closeButton;

    private Dictionary<int, Vector2> activePointers = new();
    private Dictionary<int, Vector2> prevPointers = new();

    private Vector2 position;
    private float scale = 1f;
    private float lastPinchDistance;
    private float lastTapTime;
    private bool dragging;
    private bool isPinching;

    private Vector2 lastTapPosition;
    private Vector2 lastPointerPos;

    protected override void OnInitialize()
    {
        EnhancedTouchSupport.Enable(); // required for Touch.activeTouches

        overlayRoot = Root.Q<VisualElement>("overlay");
        overlayRoot.style.display = DisplayStyle.None; // hide the UI by default
        viewport = Root.Q<VisualElement>("viewport");
        imageElement = Root.Q<Image>("image");

        closeButton = Root.Q<Button>("closebutton");
        closeButton?.RegisterCallback<ClickEvent>(_ => Close());

        viewport?.RegisterCallback<ClickEvent>(_ => Close()); // when the user clicks outside of the image close the popup

        imageElement.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation(); // stop the click event from bubbling up to parent
        });
        imageElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        imageElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        imageElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
        imageElement.RegisterCallback<WheelEvent>(OnWheel);

        ResetView();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    public void SetPayload(object payload)
    {
        _pending = payload as Texture2D;
    }

    public override void OnEnter()
    {
        if (_pending != null)
        {
            float aspect = (float)_pending.width / _pending.height;

            imageElement.style.width = Length.Percent(100);
            imageElement.style.height = StyleKeyword.Auto;
            imageElement.style.aspectRatio = aspect;

            //imageElement.style.alignSelf = Align.Center;

            imageElement.image = _pending;
        }

        overlayRoot.style.display = DisplayStyle.Flex;
    }

    public override void OnExit()
    {
        overlayRoot.style.display = DisplayStyle.None;
        imageElement.image = null;
        _pending = null;
    }

    public void Close()
    {
        UIManager.Instance.Pop();
    }

    public void SetTexture(Texture2D tex)
    {
        imageElement.image = tex;
        ResetView();
    }

    public void ResetView()
    {
        scale = 1f;
        position = Vector2.zero;
        Apply();
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (debug) Debug.Log($"Pointer down with id : {evt.pointerId}! ActivePoints : {activePointers.Count}");

        activePointers[evt.pointerId] = evt.position;

        // A second (or later) pointer landing means this is a pinch/multi-touch
        // gesture starting, not a tap. Bail out before any tap logic runs.
        if (activePointers.Count > 1)
        {
            isPinching = true;
            dragging = false;
            return;
        }

        prevPointers.TryGetValue(evt.pointerId, out Vector2 prevPosition); // get the prev position for that point id

        bool isDoubleTap =
            !isPinching &&
            (Time.time - lastTapTime) < doubleTapTime &&
            Vector2.Distance(evt.position, prevPosition) < (minDistanceBetweenTaps * DpiScale());

        //if (debug) Debug.Log($"DoubleTap is : {isDoubleTap}. \nisPinching : {isPinching}\nTime : {Time.time - lastTapTime}\nDistance : {Vector2.Distance(evt.position, lastTapPosition)}\n Dpi scale : {DpiScale()}");
        if (debug) Debug.Log($"DoubleTap is : {isDoubleTap}. \nisPinching : {isPinching}\nTime : {(Time.time - lastTapTime) < doubleTapTime} | {Time.time - lastTapTime}\nDistance : {Vector2.Distance(evt.position, prevPosition) < (minDistanceBetweenTaps * DpiScale())} | {Vector2.Distance(evt.position, prevPosition)}\n Dpi scale : {DpiScale()}");

        if (isDoubleTap)
        {
            if (scale > minZoom + 0.01f)
            {
                ResetView();
            }
            else
            {
                float targetScale = Mathf.Clamp(scale + defaultZoomValue, minZoom, maxZoom);
                ZoomTowards(evt.position, targetScale);
            }

            // consume this tap so a fast third tap doesn't immediately chain into another double-tap
            lastTapTime = 0f;
        }
        else
        {
            lastTapTime = Time.time;
        }

        lastTapPosition = evt.position;

        dragging = true;
        //lastPointerPos = evt.position;
        prevPointers[evt.pointerId] = evt.position;
        imageElement.CapturePointer(evt.pointerId);
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!dragging || isPinching || Touch.activeTouches.Count >= 2) return;
        Vector2 delta = (Vector2)evt.position - prevPointers[evt.pointerId];
        position += delta;
        //lastPointerPos = evt.position;
        prevPointers[evt.pointerId] = evt.position;
        Apply();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (debug) Debug.Log($"Pointer Up with id : {evt.pointerId}! ActivePoints : {activePointers.Count}");

        activePointers.Remove(evt.pointerId);
        
        imageElement.ReleasePointer(evt.pointerId);
        
        if (activePointers.Count == 1)
        {
            isPinching = false;
            dragging = true; // as we only have one finger we want to be able drag it around
        }

        else if (activePointers.Count == 0)
        {
            dragging = false; // as we now have no fingers on the screen, dragging should be falses
            
            // prevent the finger-lift that ends a pinch from starting a false double-tap window
            //lastTapTime = 0f;
        }
    }

    void OnWheel(WheelEvent evt)
    {
        scale = Mathf.Clamp(scale - evt.delta.y * wheelZoomSpeed, minZoom, maxZoom);
        Apply();
    }

    void Update()
    {
        if (Root != null)
        {
            //if (Touch.activeTouches.Count == 2)
            if (activePointers.Count == 2)
            {
                isPinching = true;
                HandlePinch();
            }
        }
    }

    void HandlePinch()
    {
        var t0 = Touch.activeTouches[0];
        var t1 = Touch.activeTouches[1];
        float currentDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);

        if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            t1.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            lastPinchDistance = currentDistance;
            return;
        }

        float delta = currentDistance - lastPinchDistance;
        float dpiAdjustedDelta = delta / DpiScale(); // normalize pixel delta against the dpi
        scale = Mathf.Clamp(scale + dpiAdjustedDelta * pinchSpeed, minZoom, maxZoom);
        lastPinchDistance = currentDistance;
        Apply();
    }

    // Zooms so that the point under screenPoint stays fixed on screen,
    // i.e. the image appears to zoom in/out around the tap location.
    void ZoomTowards(Vector2 screenPoint, float targetScale)
    {
        if (viewport == null || scale <= 0f)
        {
            scale = targetScale;
            Apply();
            return;
        }

        Vector2 viewportCenter = viewport.worldBound.center;
        Vector2 focal = screenPoint - viewportCenter; // tap point relative to viewport center

        float oldScale = scale;
        scale = targetScale;

        // Solve for the position that keeps `focal` pointing at the same image-space
        // location after the scale change.
        position = focal + (position - focal) * (scale / oldScale);

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
        float maxX = Mathf.Max(0, (viewport.resolvedStyle.width * scale - viewport.resolvedStyle.width) / 2f);
        float maxY = Mathf.Max(0, (viewport.resolvedStyle.height * scale - viewport.resolvedStyle.height) / 2f);
        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        position.y = Mathf.Clamp(position.y, -maxY, maxY);
    }

    float DpiScale()
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) return 1f; // if the dpi is unknown/returns nothing, then we want to set it to 1 so it has no effect on our pixel calculations.
        return dpi / referenceDpi;
    }
}
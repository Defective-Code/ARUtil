using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(UIDocument))]
public class ImageViewerUIT : MonoBehaviour
{
    public float minZoom = 1f;
    public float maxZoom = 4f;
    public float pinchSpeed = 0.01f;
    public float wheelZoomSpeed = 0.05f;
    public float doubleTapTime = 0.3f;

    [Header("Overlay root - the element to show/hide")]
    public VisualElement overlayRoot;

    //private VisualElement imageElement;
    private Image imageElement;
    private VisualElement viewport;
    private Button closeButton;
    private Vector2 position;
    private float scale = 1f;
    private float lastPinchDistance;
    private float lastTapTime;
    private bool dragging;
    private Vector2 lastPointerPos;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable(); // required for Touch.activeTouches

        var root = GetComponent<UIDocument>().rootVisualElement;
        overlayRoot = root.Q<VisualElement>("overlay");
        overlayRoot.style.display = DisplayStyle.None; // hide the UI by default
        viewport = root.Q<VisualElement>("viewport");
        //imageElement = root.Q<VisualElement>("image");
        imageElement = root.Q<Image>("image");

        closeButton = root.Q<Button>("closebutton");
        //closeButton.style.display = DisplayStyle.None;
        closeButton?.RegisterCallback<ClickEvent>(_ => Close());

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

    public void Open(Texture2D tex)
    {
        Debug.Log("Running Open - Opening the Image Viewer UI");

        Debug.Log($"overlayRoot: {overlayRoot}");
        Debug.Log($"imageElement: {imageElement}");
        Debug.Log($"viewport: {viewport}");
        Debug.Log($"closeButton: {closeButton}");

        //imageElement.style.backgroundImage = new StyleBackground(tex);
        imageElement.image = tex;
        overlayRoot.style.display = DisplayStyle.Flex;
        ResetView();

        //closeButton.style.display = DisplayStyle.Flex;
    }

    public void Close()
    {
        Debug.Log("Running Close - Closing the Image Viewer UI");

        overlayRoot.style.display = DisplayStyle.None;
        //closeButton.style.display = DisplayStyle.None;
    }

    public void SetTexture(Texture2D tex)
    {
        //imageElement.style.backgroundImage = new StyleBackground(tex);
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
        if (Time.time - lastTapTime < doubleTapTime)
        {
            ResetView();
        }
        lastTapTime = Time.time;

        dragging = true;
        lastPointerPos = evt.position;
        imageElement.CapturePointer(evt.pointerId);
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!dragging || Touch.activeTouches.Count >= 2) return;
        Vector2 delta = (Vector2)evt.position - lastPointerPos;
        position += delta;
        lastPointerPos = evt.position;
        Apply();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        dragging = false;
        imageElement.ReleasePointer(evt.pointerId);
    }

    void OnWheel(WheelEvent evt)
    {
        scale = Mathf.Clamp(scale - evt.delta.y * wheelZoomSpeed, minZoom, maxZoom);
        Apply();
    }

    void Update()
    {
        if (Touch.activeTouches.Count == 2)
            HandlePinch();
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
        scale = Mathf.Clamp(scale + delta * pinchSpeed, minZoom, maxZoom);
        lastPinchDistance = currentDistance;
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
}
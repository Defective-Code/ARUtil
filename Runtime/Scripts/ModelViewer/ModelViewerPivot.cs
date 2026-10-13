using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ModelViewerPivot : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;   // MainCamera (child of this object)

    [Header("Orbit Settings")]
    public float rotationSpeed = 5f;
    public float autoRotationSpeed = 20f;
    public bool autoRotate = false;

    [Header("Touch Settings")]
    public float touchRotationSpeed = 0.25f;
    public float touchZoomSpeed = 0.005f;

    [Header("Zoom Settings")]
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float zoomSpeed = 2f;

    [Header("Vertical Limits")]
    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw, pitch, distance;
    private float startYaw, startPitch, startDistance;
    private float lastPinchDistance;

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;

        Vector3 angles = transform.eulerAngles;
        yaw = startYaw = angles.y;
        pitch = startPitch = ClampAngle(angles.x, minPitch, maxPitch);

        distance = startDistance = Mathf.Clamp(
            cameraTransform.localPosition.magnitude, minDistance, maxDistance);

        cameraTransform.localRotation = Quaternion.identity; // camera looks down pivot's +Z
    }

    private void Update()
    {
        if (autoRotate)
            yaw += autoRotationSpeed * Time.deltaTime;
        else if (Touch.activeTouches.Count > 0)
            HandleTouch();
        else
            HandleMouse();

        HandleScroll();
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
    }

    private void HandleMouse()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * rotationSpeed * 0.1f;
        yaw += delta.x;
        pitch = ClampAngle(pitch - delta.y, minPitch, maxPitch);
    }

    private void HandleTouch()
    {
        var touches = Touch.activeTouches;

        if (touches.Count == 1)
        {
            var t = touches[0];
            if (t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.delta * touchRotationSpeed;
                yaw += delta.x;
                pitch = ClampAngle(pitch - delta.y, minPitch, maxPitch);
            }
        }
        else if (touches.Count >= 2)
        {
            float current = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);

            if (touches[0].phase == TouchPhase.Began || touches[1].phase == TouchPhase.Began)
                lastPinchDistance = current;
            else if (touches[0].phase == TouchPhase.Moved || touches[1].phase == TouchPhase.Moved)
            {
                float pinchDelta = current - lastPinchDistance;
                lastPinchDistance = current;
                // fingers apart = zoom in = smaller distance
                distance = Mathf.Clamp(distance - pinchDelta * touchZoomSpeed, minDistance, maxDistance);
            }
        }
    }

    private void HandleScroll()
    {
        if (Mouse.current == null || Touch.activeTouches.Count > 0) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            distance = Mathf.Clamp(distance * (1f - scroll * zoomSpeed * 0.001f), minDistance, maxDistance);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        angle = Mathf.Repeat(angle + 180f, 360f) - 180f;   // normalise to -180..180
        return Mathf.Clamp(angle, min, max);
    }

    public void ResetView()
    {
        yaw = startYaw;
        pitch = startPitch;
        distance = startDistance;
    }

    public void ToggleAutoRotation(bool value) => autoRotate = value;
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UIElements;

public class ModelViewerOverlay : UIViewBehaviour, IPayloadReceiver
{
    [SerializeField] private bool debug;

    //private float referenceDpi = 360f; // dpi of the test device. Basically, once the "Feel" for the test device is correct, this will adjust the pixel values so that that feel auto translates to other dpi's/screen sizes by nomralizing against the dpi of the device used to "tune" the values

    //private GameObject _model; // model to view

    // Variables to change for feel
    //public float minZoom = 1f;
    //public float maxZoom = 4f;
    //public float pinchSpeed = 0.01f;
    //public float wheelZoomSpeed = 0.05f;
    //public float doubleTapTime = 0.3f;
    //public float minDistanceBetweenTaps = 50f; // max screen distance between successive taps to count as a double-tap (rather than two pinch fingers landing)
    //public float defaultZoomValue = 15f; // added to scale (then clamped) when double tapping in

    // UI Elements 
    [Header("Overlay root - the element to show/hide")]
    public VisualElement overlayRoot;
    private VisualElement modelOverlay;
    private Button closeButton;

    //private Dictionary<int, Vector2> activePointers = new();
    ////private Dictionary<int, Vector2> prevPointers = new(); // stores the last position of a given pointer (this is for double tap purposes)

    //private Vector3 initialPosition; // record the starting position of the model so we can modify zoom etc based off of this

    //private Quaternion rotation; // the rotation applied to the model
    //private float rotationAngle;
    //private float scale = 1f;
    //private float lastPinchDistance;
    //private float lastTapTime;
    //private bool dragging;
    //private bool isPinching;

    // to set the model to view
    public void SetPayload(object payload)
    {
        //_model = payload as GameObject;
    }


    protected override void OnInitialize()
    {
        EnhancedTouchSupport.Enable(); // required for Touch.activeTouches

        overlayRoot = Root.Q<VisualElement>("overlay");
        overlayRoot.style.display = DisplayStyle.None; // hide the UI by default
        modelOverlay = Root.Q<VisualElement>("model-overlay");

        closeButton = Root.Q<Button>("close-button");
        closeButton?.RegisterCallback<ClickEvent>(_ => Close());


        //modelOverlay.RegisterCallback<ClickEvent>(evt =>
        //{
        //    evt.StopPropagation(); // stop the click event from bubbling up to parent
        //});
        //modelOverlay.RegisterCallback<PointerDownEvent>(OnPointerDown);
        //modelOverlay.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        //modelOverlay.RegisterCallback<PointerUpEvent>(OnPointerUp);
        //modelOverlay.RegisterCallback<WheelEvent>(OnWheel);

        //ResetView();
    }

    public override void OnEnter()
    {
        overlayRoot.style.display = DisplayStyle.Flex;
        //initialPosition = new Vector3(_model.transform.localPosition.x, _model.transform.localPosition.y, _model.transform.localPosition.z);
    }

    public override void OnExit()
    {
        overlayRoot.style.display = DisplayStyle.None;
        //Destroy(_model); // destroy the copy of the gameobject we are viewing
        //_model = null;
    }

    public void Close()
    {
        UIManager.Instance.Pop();
        FindFirstObjectByType<ModelViewerLauncher>().Close();
    }

    //public void ResetView()
    //{
    //    scale = 1f;
    //    _model.transform.rotation = Quaternion.identity;
    //    //Apply();
    //}


    //void OnPointerDown(PointerDownEvent evt)
    //{
    //    if (debug) Debug.Log($"Pointer down with id : {evt.pointerId}! Model position : {_model.transform.position}! Model rotation : {_model.transform.localRotation}");

    //    activePointers[evt.pointerId] = evt.position;

    //    // A second (or later) pointer landing means this is a pinch/multi-touch
    //    // gesture starting, not a tap. Bail out before any tap logic runs.
    //    if (activePointers.Count > 1)
    //    {
    //        isPinching = true;
    //        dragging = false;
    //        return;
    //    }

    //    dragging = true;

    //    modelOverlay.CapturePointer(evt.pointerId);
    //}

    //void OnPointerMove(PointerMoveEvent evt)
    //{
    //    if (!dragging || isPinching || activePointers.Count >= 2 || evt.pointerId > 1) return;

    //    if (debug) Debug.Log($"Point moved with id : {evt.pointerId}! Model position : {_model.transform.position}! Model rotation : {_model.transform.localRotation}");

    //    activePointers.TryGetValue(evt.pointerId, out Vector2 prevPointerPos); // activePointers stores the last recorded position of a given pointer on the screen
    //    Vector2 delta = (Vector2)evt.position - prevPointerPos;
    //    //position += delta;

    //    float distanceFromCam = _model.transform.localPosition.z; // get the distance from the model to the camera

    //    //Matrix4x4 m = Camera.main.cameraToWorldMatrix;
    //    //Vector2 v1 = m.MultiplyPoint(evt.position);
    //    //Vector2 v2 = m.MultiplyPoint(prevPointerPos);

    //    Vector3 point1 = Camera.main.ScreenToWorldPoint(new Vector3(evt.position.x, evt.position.y, 0));
    //    //Vector3 point1 = Camera.main.ScreenToWorldPoint(new Vector3(evt.position.x, evt.position.y, distanceFromCam));
    //    Vector3 point2 = Camera.main.ScreenToWorldPoint(new Vector3(prevPointerPos.x, prevPointerPos.y, 0));
    //    //Vector3 point2 = Camera.main.ScreenToWorldPoint(new Vector3(prevPointerPos.x, prevPointerPos.y, distanceFromCam));
    //    //rotationAngle = Vector3.Angle(point1, point2);
    //    //rotation = Quaternion.AngleAxis(angle, Vector3.up);

    //    // getting the vector between each point and the model
    //    Vector3 line1 = _model.transform.position - point1;
    //    Vector3 line2 = _model.transform.position - point2;

    //    if (debug) Debug.Log($"Line1 : {line1} | Line2 : {line2}");

    //    //rotationAngle = Vector3.Angle(line1, line2);
    //    rotation = Quaternion.FromToRotation(line1, line2);

    //    if (debug)
    //    {
    //        LineRenderer lineRenderer = GetComponent<LineRenderer>();

    //        if (lineRenderer == null)
    //        {
    //            lineRenderer = gameObject.AddComponent<LineRenderer>();
    //            lineRenderer.widthMultiplier = 0.05f;
    //            //lineRenderer.material = new Material(Shader.Find("Sprite-Unlit-Default"));
    //            lineRenderer.useWorldSpace = true;
    //            lineRenderer.positionCount = 2;
    //        }

    //        var testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //        testCube.transform.position = point1;
    //        testCube.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

    //        Vector3[] points = new Vector3[2];
    //        points[0] = point1;
    //        points[1] = _model.transform.position;

    //        lineRenderer.SetPositions(points);

    //        //Vector3 dir1 = _model.transform.position - point1;
    //        //Vector3 dir2 = _model.transform.position - point2;

    //        //Ray ray = new Ray(point1, dir1);
    //        //Ray ray2 = new Ray(point2, dir2);

    //        //Debug.DrawLine(point1, _model.transform.position, Color.red, 10.0f);
    //        //Debug.DrawLine(point2, _model.transform.position, Color.red, 10.0f);
    //    }

    //    if (debug) Debug.Log($"Rotation in euler angles : {rotation.eulerAngles}");

    //    activePointers[evt.pointerId] = evt.position;

    //    //_model.transform.Rotate(Vector3.up, rotationAngle, Space.Self);
    //    _model.transform.Rotate(rotation.eulerAngles, Space.Self);
    //}

    //void OnPointerUp(PointerUpEvent evt)
    //{
    //    if (debug) Debug.Log($"Pointer Up with id : {evt.pointerId}! Model position : {_model.transform.position}! Model rotation : {_model.transform.localRotation}");

    //    activePointers.Remove(evt.pointerId);

    //    modelOverlay.ReleasePointer(evt.pointerId);

    //    if (activePointers.Count == 1)
    //    {
    //        isPinching = false;
    //        dragging = true; // as we only have one finger we want to be able drag it around
    //    }

    //    else if (activePointers.Count == 0)
    //    {
    //        dragging = false; // as we now have no fingers on the screen, dragging should be falses

    //        // prevent the finger-lift that ends a pinch from starting a false double-tap window
    //        //lastTapTime = 0f;
    //    }
    //}

    ////void OnWheel(WheelEvent evt)
    ////{
    ////    scale = Mathf.Clamp(scale - evt.delta.y * wheelZoomSpeed, minZoom, maxZoom);
    ////    Apply();
    ////}

    //void Update()
    //{
    //    if (Root != null)
    //    {
    //        //if (Touch.activeTouches.Count == 2)
    //        if (activePointers.Count == 2)
    //        {
    //            isPinching = true;
    //            HandlePinch();
    //        }
    //    }
    //}

    //void HandlePinch()
    //{
    //    var t0 = Touch.activeTouches[0];
    //    var t1 = Touch.activeTouches[1];
    //    float currentDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);

    //    if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
    //        t1.phase == UnityEngine.InputSystem.TouchPhase.Began)
    //    {
    //        lastPinchDistance = currentDistance;
    //        return;
    //    }

    //    float delta = currentDistance - lastPinchDistance;
    //    float dpiAdjustedDelta = delta / DpiScale(); // normalize pixel delta against the dpi
    //    scale = Mathf.Clamp(scale + dpiAdjustedDelta * pinchSpeed, minZoom, maxZoom);
    //    lastPinchDistance = currentDistance;

    //    _model.transform.localPosition = new Vector3(0, 0, initialPosition.z / scale);
    //}

    //float DpiScale()
    //{
    //    float dpi = Screen.dpi;
    //    if (dpi <= 0f) return 1f; // if the dpi is unknown/returns nothing, then we want to set it to 1 so it has no effect on our pixel calculations.
    //    return dpi / referenceDpi;
    //}

}

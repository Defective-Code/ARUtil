using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[DefaultExecutionOrder(-100)]
public class UserTouchManager : MonoBehaviour
{
    public static UserTouchManager Instance { get; private set; }

    [Header("Edge blocking")]
    [SerializeField, Range(0f, 0.15f)] private float edgeFraction = 0.03f;

    [Header("Ignore Stationary")]
    [SerializeField, Range(1, 100)] private int stationaryLimit = 30;

    public bool debug;

    private readonly Dictionary<int, bool> decisions = new(); // touchId -> accepted
    private readonly HashSet<int> seenThisFrame = new();
    private readonly List<int> staleIds = new();
    private readonly List<Touch> activeTouches = new();
    private readonly Dictionary<int, int> stationaryCount = new(); // dictionary to store count of stationary phases for a given touch - if it exceeds a given number for a touch then we know the user is holding a finger down for the most part, so we ignore the touch - for now

    /// All touches that passed the edge filter this frame.
    public IReadOnlyList<Touch> ActiveTouches => activeTouches;

    public bool IsAccepted(int touchId) =>
        decisions.TryGetValue(touchId, out bool accepted) && accepted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        TouchFilter.edgeFraction = edgeFraction;
    }

    private void OnValidate()
    {
        // Lets you tweak the margin live from the inspector in play mode
        TouchFilter.edgeFraction = edgeFraction;
    }

    private void OnEnable()
    {
        if (Instance == this) EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        if (Instance == this) EnhancedTouchSupport.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        activeTouches.Clear();
        seenThisFrame.Clear();

        foreach (Touch touch in Touch.activeTouches)
        {
            int id = touch.touchId;
            seenThisFrame.Add(id);

            // Decide once, the first time we see this touch.
            if (!decisions.TryGetValue(id, out bool accepted))
            {
                accepted = TouchFilter.IsValid(touch);
            
                if (!accepted) stationaryCount[id] = 0; // start count of stationary phase if the touch is in a boundary area

                //decisions[id] = accepted;
                decisions[id] = true; // by default all touches are allowed, only disallow when the user is touching the outer edge AND hasnt moved in 30 frames.

                if (debug)
                    Debug.Log($"Touch {id} started at {touch.startScreenPosition}: accepted={accepted}");
            }

            if (accepted) activeTouches.Add(touch);
        }

        foreach (Touch touch in activeTouches)
        {
            //if (debug) Debug.Log($"Touchphase of touch with id : {id} is {touch.phase}");

            if (touch.phase == TouchPhase.Stationary)
            {
                int id = touch.touchId;
                if (stationaryCount.TryGetValue(id, out var currentCount)) // only  if we have started a counter (meaning if the touch was in the disallowed zone)
                {
                    stationaryCount[id] = currentCount + 1;

                    if (stationaryCount[id] > stationaryLimit)
                    {
                        if (debug) Debug.Log($"Removed touch with touch id : {id} due to stationary count being too high");
                        decisions[id] = false; // make it so the touch is invalid
                        stationaryCount.Remove(id); // remove the entry as we have no judged it as not a valid touch
                    }
                }
            }
        }

        // Forget touches that are gone (Ended shows for one update, then drops out).
        staleIds.Clear();
        foreach (int id in decisions.Keys)
            if (!seenThisFrame.Contains(id)) staleIds.Add(id);
        foreach (int id in staleIds)
        {
            decisions.Remove(id);
            stationaryCount.Remove(id);
        }
    }
}
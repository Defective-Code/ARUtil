using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class UserTouchRaycastManager : MonoBehaviour
{

    public static UserTouchRaycastManager Instance {  get; set; }

    public bool debug;

    // Dictionary linking names to Touch Objects, this represents all the objects in a scene we want to be interactable with a touch
    private Dictionary<string, TouchObject> d_namesToInterface = new Dictionary<string, TouchObject>(); 

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (debug)
        {
            foreach (string t in d_namesToInterface.Keys)
            {
                Debug.Log(t);
            }
        }

        // if we have added no interactable objects then don't bother doing the raycast checks
        if (d_namesToInterface.Count <= 0)
        {
            if (debug) Debug.LogWarning("Names To Interface was empty - We have not registered any objects to be interactable in the dictionary");
            return;
        }

        // If we have a UI element open then we also dont need to both doing raycasts as we dont want any touches going "through" the UI
        if (UIManager.Instance.IsUIElementOpen)
        {
            if (debug) Debug.LogWarning("A UI element was open so blocking any raycasts for touchObjects");
            return;
        }

        //var activeTouches = Touch.activeTouches;
        var activeTouches = UserTouchManager.Instance.ActiveTouches;

        if (activeTouches.Count > 0 && (activeTouches[0].phase == TouchPhase.Ended))
        {
            var activeTouch = activeTouches[0];

            //if (!TouchFilter.IsValid(activeTouch)) return;

            Ray raycast = Camera.main.ScreenPointToRay(activeTouch.screenPosition);
            RaycastHit raycastHit;

            if (Physics.Raycast(raycast, out raycastHit))
            {


                // debug to visualize the normal of the surface that was hit
                //Debug.DrawLine(raycastHit.normal, raycastHit.normal + new Vector3(5, 5, 5), Color.red, 10f);
                //Debug.LogError($"Raycast normal : {raycastHit.normal}");

                GameObject hitObject = raycastHit.collider.gameObject;

                Debug.Log($"{hitObject.name} was hit");

                // check that the dictionary is not empty so we actually spawn things and give the correct feedback and that the hit gameobject has a pair gameobject to enable
                //if (namesToObjects.GetCount() > 0 && namesToObjects.ContainsKey(hitObject) && namesToObjects.GetValue(hitObject) != null) {\\

                // check that the list of interactable gameobjects is not empty so the user has something to interact with
                //Debug.Log($"{interactableObjects.Count} interactable objects");

                if (d_namesToInterface.TryGetValue(hitObject.name, out TouchObject to))
                {
                    Debug.Log($"{hitObject.name} was invoked!");
                    //to.OnTouch?.Invoke();
                    to.RaiseTouchEvent();
                }
            }
        }
    }

    public void RegisterInteractable(TouchObject touchObject)
    {
        if (touchObject == null) return;

        //interactableObjects.Add(touchObject);
        d_namesToInterface[touchObject.gameObject.name] = touchObject; // overwrite is fine/expected
    }

    public void UnregisterInteractable(TouchObject touchObject)
    {
        if (touchObject == null) return;

        //interactableObjects.Remove(touchObject);
        if (d_namesToInterface.TryGetValue(name, out var existing) && existing == touchObject)
            d_namesToInterface.Remove(name);
    }
}

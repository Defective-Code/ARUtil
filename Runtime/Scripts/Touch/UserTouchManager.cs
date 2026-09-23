using System.Collections.Generic;
using TMPro;
using Unity.IO.Archive;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// class that handles all user touch interactions with specified 3d objects (3d objects with the ... class)
public class UserTouchManager : MonoBehaviour
{

    public static UserTouchManager Instance { get; private set; }

    // The list of objects to be touchable
    public List<TouchObject> interactableObjects = new List<TouchObject>();

    private Dictionary<string, TouchObject> d_namesToInterface = new Dictionary<string, TouchObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();

        if (interactableObjects.Count <= 0)
        {
            Debug.LogWarning("Interactable objects didn't have any objects in it");
            return;
        }

        d_namesToInterface.Clear();
        // populate the lookup for interactable objects
        foreach(var t in interactableObjects)
        {
            d_namesToInterface.Add(t.gameObject.name, t);
        }
    }

    void OnDisable()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    // Update is called once per frame
    void Update()
    {

        // if we have added no interactable objects then don't bother doing the raycast checks
        if (interactableObjects.Count <= 0)
        {
            Debug.LogWarning("Interactable objects didn't have any objects in it");
            return;
        }

        var activeTouches = Touch.activeTouches;

        //if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Began))
        if (activeTouches.Count > 0 && (activeTouches[0].phase == TouchPhase.Began))
        {

            var activeTouch = activeTouches[0];

            //Debug.Log("Touch was detected");

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
                Debug.Log($"{interactableObjects.Count} interactable objects");

                if (d_namesToInterface.TryGetValue(hitObject.name, out TouchObject to))
                {
                    Debug.Log($"{hitObject.name} was invoked!");
                    to.OnTouch?.Invoke();
                }
            }
        }
    }

    // UserTouchManager.cs
    public void RegisterInteractable(TouchObject touchObject)
    {
        if (touchObject == null) return;

        interactableObjects.Add(touchObject);
        d_namesToInterface[touchObject.gameObject.name] = touchObject; // overwrite is fine/expected
    }

    public void UnregisterInteractable(TouchObject touchObject)
    {
        if (touchObject == null) return;

        interactableObjects.Remove(touchObject);
        d_namesToInterface.Remove(touchObject.gameObject.name);
    }
}

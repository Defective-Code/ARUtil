using System;
using UnityEngine;
using UnityEngine.Events;

// Specfies the methods needed for a given 3d object to be "touchable" 
public class TouchObject : MonoBehaviour
{
    //public Action OnTouch;
    public UnityEvent OnTouch;

    private void Awake()
    {
        if (UserTouchManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: UserTouchManager.Instance was null on registration!");
            return;
        }
        UserTouchManager.Instance.RegisterInteractable(this);

        if (OnTouch == null)
        {
            Debug.LogWarning($"OnTouch was null for {gameObject.name}");
            OnTouch = new UnityEvent();
        }
    }
}

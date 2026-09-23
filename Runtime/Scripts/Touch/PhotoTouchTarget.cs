using UnityEngine;

// Sits alongside TouchObject on any 3D object that should open
// the ImageViewer when touched.
[RequireComponent(typeof(TouchObject))]
public class PhotoTouchTarget : MonoBehaviour
{
    [SerializeField] private Texture2D photo;

    private TouchObject _touchObject;

    void OnEnable()
    {
        Debug.Log("Phototouch Target being enabled!");

        _touchObject = GetComponent<TouchObject>();
        _touchObject.OnTouch.AddListener(HandleTouched);
    }

    void OnDisable()
    {
        _touchObject.OnTouch.RemoveListener(HandleTouched);
    }

    private void HandleTouched()
    {
        Debug.Log($"Fired for {gameObject.name}");
        UIManager.Instance.Push(UIScreen.ImageViewer, photo);
    }
}
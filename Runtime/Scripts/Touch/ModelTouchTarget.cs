using UnityEngine;

public class ModelTouchTarget : MonoBehaviour
{
    [SerializeField] Vector3 defaultPosition = new Vector3(0, 0, -0.5f);

    private TouchObject _touchObject;

    private GameObject viewCopy; // store reference to the copy of this GameObject that is used for viewing purposes 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        _touchObject = GetComponent<TouchObject>();

        _touchObject.OnTouch += ModelViewer;
    }

    void OnDisable()
    {
        _touchObject.OnTouch -= (ModelViewer);
    }

    private void ModelViewer()
    {
        viewCopy = Instantiate(gameObject); // create a copy of this gameObject
        viewCopy.transform.SetParent(Camera.main.transform, false);
        viewCopy.gameObject.transform.localPosition = defaultPosition; // make the copy sit 0.5u in front of the camera

        UIManager.Instance.Push(UIScreen.ModelViewer, viewCopy);
    }


}

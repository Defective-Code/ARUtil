using UnityEngine;

public class SimpleBillboard : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(cam.transform);
        transform.Rotate(0, 180, 0); // rotate 180 degrees the so the rendering side faces the camera;
    }
}
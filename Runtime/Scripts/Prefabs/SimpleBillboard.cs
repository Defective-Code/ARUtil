using UnityEngine;

public class SimpleBillboard : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // Horizontal direction from this object to the camera
        Vector3 toCam = cam.transform.position - transform.position;
        toCam.y = 0f;

        // Horizontal direction the visible side currently faces (the -Z side, as in your original code)
        Vector3 facing = -transform.forward;
        facing.y = 0f;

        if (toCam.sqrMagnitude < 1e-6f || facing.sqrMagnitude < 1e-6f) return;

        // Rotate about the world up axis by the angle between the two
        float delta = Vector3.SignedAngle(facing, toCam, Vector3.up);
        transform.rotation = Quaternion.AngleAxis(delta, Vector3.up) * transform.rotation;
    }
}
using UnityEngine;

public class ZoomController : MonoBehaviour
{
    public Transform target;
    public float zoomSpeed = 10f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        Vector3 dir = transform.forward * scroll;
        float distance = Vector3.Distance(transform.position + dir, target.position);
        if (distance >= minDistance && distance <= maxDistance)
            transform.position += dir;
    }
}
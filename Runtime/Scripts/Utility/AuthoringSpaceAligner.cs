using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AuthoringSpaceAligner : MonoBehaviour
{
    void OnValidate()
    {
        Align();
    }

    void Update()
    {
        // check saying not to run this if the application is actually running, we only want this in the editor
        if (!Application.isPlaying)
        {
            Align();
        }
    }

    void Align()
    {
        if (transform.parent == null) return;

        Quaternion targetLocalRotation = Quaternion.Inverse(transform.parent.localRotation);

        if (transform.localRotation != targetLocalRotation )
        {
            transform.localRotation = targetLocalRotation;
        }
    }
}

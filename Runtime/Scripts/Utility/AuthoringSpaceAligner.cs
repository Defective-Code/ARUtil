using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AuthoringSpaceAligner : MonoBehaviour
{
    void OnValidate()
    {
        Align();
    }

    void Start()
    {
        // On start once, we want to offset any rotation applied to the real 
        //Align(); 
        transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        // Olny want this inside the editor, so check thats its not running 
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

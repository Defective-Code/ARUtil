using NUnit.Framework;
using UnityEngine;
using UnityEngine.Android;

// class that checks permissions on awake - attach to a GameObject such as the camera
public class PermissionsChecker : MonoBehaviour
{
    void Awake()
    {
        CheckPermissions();    
    }

    private void CheckPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.LogWarning("Camera permission was not enabled");
            Permission.RequestUserPermission(Permission.Camera);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.LogWarning("Location permission was not enabled");
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        // Haven't enabled permission to Microphone
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogWarning("Microphone permission was not enabled");
            Permission.RequestUserPermission(Permission.Microphone);
        }
    }
}

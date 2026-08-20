using NUnit.Framework;
using UnityEngine;
using UnityEngine.Android;

public class PermissionsChecker
{
    public static void CheckPermissions()
    {
        // Haven't enabled permission to Microphone
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogWarning("Microphone permission was not enabled");
            Permission.RequestUserPermission(Permission.Microphone);
        }

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
    }
}

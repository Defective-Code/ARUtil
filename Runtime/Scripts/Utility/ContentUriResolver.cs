using UnityEngine;
using System.IO;

public static class ContentUriResolver
{
    public static string CopyContentUriToLocalFile(string contentUriString, string destFileName)
    {
        string destPath = Path.Combine(Application.persistentDataPath, destFileName);
        
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaClass helper = new AndroidJavaClass("com.hcilab.ARRecording.ContentUriHelper"))
        {
            string result = helper.CallStatic<string>(
                "copyContentUriToFile", activity, contentUriString, destPath);
            return result; // null on failure, absolute path on success
        }
    }
}

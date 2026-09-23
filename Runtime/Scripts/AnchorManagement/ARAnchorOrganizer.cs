using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARAnchorOrganizer : MonoBehaviour
{
    public AnchorData anchorData;
    public ARAnchorManager aRAnchorManager;

    // Creates an anchor and then runs the code specified by the createChild function
    public async void CreateAnchor(string key, Vector3 position, Quaternion rotation, Action<ARAnchor> createChild)
    {
        Pose pose = new Pose(position, rotation);
        var result = await aRAnchorManager.TryAddAnchorAsync(pose);

        if (result.status.IsSuccess())
        {
            var anchor = result.value;

            createChild(anchor);
            anchorData.AddAnchor(key, anchor);

        }
        else
        {
            Debug.LogError($"ARAnchorOrganizer - Failed to create the anchor for tag {key} with {result.status}");
        }
    }

    public void RemoveAnchor(string key)
    {
        if (DeleteAnchor(anchorData.Get(key)))
        {
            anchorData.RemoveAnchor(key);
        } 
        else
        {
            Debug.LogError($"Failed to remove anchor for key : {key}");
        }
    }

    private bool DeleteAnchor(ARAnchor toRemove)
    {
        var result = aRAnchorManager.TryRemoveAnchor(toRemove);

        return result;
    }

    // Loop over all the anchors and remove them
    public void ClearAnchors()
    {
        Debug.Log("Calling clear anchors");
        foreach (var key in anchorData.GetKeys())
        {
            RemoveAnchor(key);
        }
    }
}

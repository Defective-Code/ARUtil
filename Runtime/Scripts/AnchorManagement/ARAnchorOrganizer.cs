using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARAnchorOrganizer : MonoBehaviour
{
    //public static ARAnchorOrganizer Instance { get; private set; }

    public ArRoot aRRoot;

    public AnchorData anchorData;
    public ARAnchorManager aRAnchorManager;

    private HashSet<string> pendingImages = new();

    //private void Awake()
    //{
    //    if (Instance != null)
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }

    //    Instance = this;
    //    DontDestroyOnLoad(gameObject);

    //}

    private void OnEnable()
    {
        aRRoot.aRPlayback.SessionReset += ClearAnchors;
    }

    private void OnDisable()
    {
        aRRoot.aRPlayback.SessionReset -= ClearAnchors;
    }

    private void Start()
    {
        ARAnchorUIController anchorListUI = (ARAnchorUIController)UIManager.Instance?.GetUIView(UIScreen.AnchorMenu);
        anchorListUI.e_RemoveAnchor += RemoveAnchor;
    }

    private void OnDestroy()
    {
        ARAnchorUIController anchorListUI = (ARAnchorUIController)UIManager.Instance.GetUIView(UIScreen.AnchorMenu);
        anchorListUI.e_RemoveAnchor -= RemoveAnchor;
    }

    // Creates an anchor and then runs the code specified by the createChild function
    public async void CreateAnchor(string key, Vector3 position, Quaternion rotation, Action<ARAnchor> createChild)
    {
        if (!pendingImages.Add(key)) // add the key as we dont want to have duplicate anchors spanwed the same image
            return; // do not create the anchor if we are currently awaiting an anchor to be created for a given image

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
        
        pendingImages.Remove(key); // regardless if its successfully created or not, release the pending slot.
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

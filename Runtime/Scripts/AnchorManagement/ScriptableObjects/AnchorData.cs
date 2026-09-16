using UnityEngine;
using UnityEngine.XR.ARFoundation;

using System;
using System.Collections.Generic;

// SO to manage all anchor related functions
[CreateAssetMenu(fileName = "AnchorData", menuName = "Scriptable Objects/AnchorData")]
public class AnchorData : ScriptableObject
{
    struct imageNameAnchorPair {
        string name;
        ARAnchor anchor;
    }

    struct tagIdAnchorPair
    {
        int id;
        ARAnchor anchor;
    }

    //public Dictionary<Guid, ARAnchor> d_ImageAnchor = new Dictionary<Guid, ARAnchor>(); // store image guid and associated anchor
    private Dictionary<string, ARAnchor> d_ImageAnchor = new Dictionary<string, ARAnchor>(); // store image name and associated anchor
    private Dictionary<int, ARAnchor> d_AprilTagAnchor = new Dictionary<int, ARAnchor>();

    public event Action AnchorsUpdated; // event for when anchors changed 

    public void AddAnchor(string name, ARAnchor anchor)
    {
        d_ImageAnchor.Add(name, anchor);
        AnchorsUpdated?.Invoke();
    }

    // method to add an AprilTag anchor
    public void AddAnchor(int id, ARAnchor anchor)
    {
        d_AprilTagAnchor.Add(id, anchor);
        AnchorsUpdated?.Invoke();
    }

    public void RemoveAnchor(string name)
    {
        //DeleteAnchor(d_ImageAnchor[name]);
        d_ImageAnchor.Remove(name);
        AnchorsUpdated?.Invoke();
    }

    public void RemoveAnchor(int id)
    {
        //DeleteAnchor(d_AprilTagAnchor[id]);
        d_AprilTagAnchor.Remove(id);
        AnchorsUpdated?.Invoke();
    }

    //private void DeleteAnchor(ARAnchor toRemove)
    //{
    //    var result = aRAnchorManager.TryRemoveAnchor(toRemove);

    //    if (!result)
    //    {
    //        Debug.LogError($"Failed to remove anchor");
    //        return;
    //    }
    //}


    public bool ContainsKey(string name)
    {
        return d_ImageAnchor.ContainsKey(name);
    }

    public bool ContainsKey(int id)
    {
        return d_AprilTagAnchor.ContainsKey(id);
    }

    public ARAnchor Get(string name)
    {
        return d_ImageAnchor[name];
    }

    public ARAnchor Get(int id)
    {
        return d_AprilTagAnchor[id];
    }
    public void ResetAnchors()
    {
        d_ImageAnchor.Clear();
        d_AprilTagAnchor.Clear();
        AnchorsUpdated?.Invoke();
    }

    public List<string> GetKeys()
    {
        List<string> toReturn = new List<string>();
        toReturn.AddRange(d_ImageAnchor.Keys);
        //toReturn.AddRange(d_AprilTagAnchor.Keys);

        return toReturn;
    }

}

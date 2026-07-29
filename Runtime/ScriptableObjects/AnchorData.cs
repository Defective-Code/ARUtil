using UnityEngine;
using UnityEngine.XR.ARFoundation;

using System;
using System.Collections.Generic;

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

    // method to add an image anchor
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
        d_ImageAnchor.Remove(name);
        AnchorsUpdated?.Invoke();
    }

    public void RemoveAnchor(int id)
    {
        d_AprilTagAnchor.Remove(id);
        AnchorsUpdated?.Invoke();
    }

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
}

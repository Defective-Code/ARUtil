using UnityEngine;
using Unity.Properties;
using UnityEngine.XR.ARFoundation;

using System;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "DummyAnchorData", menuName = "Scriptable Objects/DummyAnchorData")]
public class DummyAnchorData : ScriptableObject
{
    //Dictionary<Guid, ARAnchor> d_ImageAnchor = new Dictionary<Guid, ARAnchor>(); // store image guid and associated anchor
    //Dictionary<int, string> d_AprilTagAnchor = new Dictionary<int, string> {
    //    {
    //        0, "Anchor"
    //    }
    //}; // store apriltag id and associated anchor

    [CreateProperty]
    public List<string> l_dummyList = new List<string> { "test" };

    [CreateProperty]
    public string dummyText = "test1";
}

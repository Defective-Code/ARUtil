using UnityEngine;
using UnityEngine.XR.ARFoundation;

using System.Collections.Generic;

public class AnchorPositioning : MonoBehaviour
{

    [SerializeField]
    AnchorData anchorData;

    [SerializeField]
    GameObject p_ToSpawn;
    [SerializeField]
    GameObject debug;

    GameObject g_spawned;

    Dictionary<string, Vector3> roomSpacePositions = new Dictionary<string, Vector3>
    {
        {"triangle_marker", new Vector3(0f, 0f, 0f) }, //plaque as origin
        {"tag_0", new Vector3(-2.3f, 0f, 0f) },
        {"tag_1", new Vector3(2.25f, 0f, 1.3f) }
    };

    readonly string[] requiredMarkers = { "tag_0", "tag_1", "triangle_marker" };

    void Awake()
    {
        //aprilIdAnchorDict = anchorData.d_AprilTagAnchor;
        //d_ImageAnchor = anchorData.d_ImageAnchor;

        anchorData.AnchorsUpdated += HandleAnchorUpdate;
    }

    void HandleAnchorUpdate()
    {
        if (!anchorData.ContainsKey(1) || !anchorData.ContainsKey("triangle_marker") || !anchorData.ContainsKey(0))
        {
            Debug.LogWarning("Have not scanned all needed targets yet");
            return; //as we ave not yet scanned all the needed positions
        }

        //Vector3 pos = Vector3.zero;
        //Quaternion rot = Quaternion.identity;

        //Vector3 roomP1 = roomSpacePositions[requiredMarkers[0]];
        //Vector3 roomP2 = roomSpacePositions[requiredMarkers[1]];
        //Vector3 roomP3= roomSpacePositions[requiredMarkers[2]];

        //Vector3 worldP1 = anchorData.Get("triangle_marker").transform.position;
        //Vector3 worldP2 = anchorData.Get(0).transform.position;
        //Vector3 worldP3 = anchorData.Get(1).transform.position;

        //Quaternion roomRot = BuildFrameRotation(roomP1, roomP2, roomP3);
        //Quaternion worldRot = BuildFrameRotation(worldP1, worldP2, worldP3);
        //Quaternion delta = worldRot * Quaternion.Inverse(roomRot);

        //rot = delta;
        //pos = worldP1 - (delta * roomP1);

        //g_spawned = GameObject.Instantiate(p_ToSpawn, pos, rot);

        //Debug.Log($"Spawed overlay at {pos} with a rot of {rot}");

        ARAnchor tag0 = anchorData.Get(0); // retrieve the anchor for aprilTag id 0
        ARAnchor tag1 = anchorData.Get(1); // retrieve the anchor for aprilTag id 1
        ARAnchor triangleImage = anchorData.Get("triangle_marker"); // reteive the anchor the triange target

        Debug.Log($"April : {tag1.transform.position} | Image : {triangleImage.transform.position} | AprilRight : {tag1.transform.right} | ImageRight : {triangleImage.transform.forward}");

        Vector3 pointFromA = tag1.transform.position + tag1.transform.right * 1.625f;
        Vector3 pointFromB = triangleImage.transform.position - triangleImage.transform.forward * 1.14f;
        Vector3 pointFromC = tag0.transform.position + tag0.transform.forward * 2.9f;

        Vector3 upFromB = triangleImage.transform.position + Vector3.up * 0.2f;

        Vector3 targetPosition = new Vector3(pointFromC.z, upFromB.y, pointFromB.z);

        //Quaternion targetRotation = Quaternion.EulerAngles(0, triangleImage.transform.rotation.eulerAngles.y + 180, 0

        g_spawned = GameObject.Instantiate(p_ToSpawn, targetPosition, triangleImage.transform.rotation);
        GameObject.Instantiate(debug, targetPosition, triangleImage.transform.rotation);

        Debug.Log($"Moved overlay to : {targetPosition}");
    }

    Quaternion BuildFrameRotation(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 fwd = (p2 - p1).normalized;
        Vector3 normal = Vector3.Cross(fwd, (p3-p1)).normalized;
        Vector3 right = Vector3.Cross(normal, fwd).normalized;
        fwd = Vector3.Cross(right, normal).normalized;
        return Quaternion.LookRotation(fwd, normal);
    }
}

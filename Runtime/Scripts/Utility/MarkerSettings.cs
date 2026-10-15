using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Class to handle transformations to the Image and content (such as the how to orient the content compared to the marker/target)
public class MarkerSettings : MonoBehaviour
{

    public bool orientRelToAnchor; // toggle to control if orientation follows parent ARAnchor or is flat. The default is flat as in the anchor is aligned with the world axes

    public bool isCrop = false; // flag for saying a given image in the the reference library is a crop of another.
    public Vector2 originalSizePx = new Vector2(4846, 3665);      // W, H of the full panel image
    public Vector2 cropPosPx;           // top-left of crop within original (x, y), y down
    public Vector2 cropSizePx;          // w, h of the crop
    //public Transform contentRoot;       // AR content to align with the full panel
    
    /// <summary>
    /// Function to align a Vector3 representing the position based on a crop of an image with the entire wider image - for a given image that is a crop, find the distances to move the transform such that the transform is now in the center of the wider "parent" image
    /// </summary>
    /// <param name="img"></param>
    public Vector3 AlignCrop(ARTrackedImage img)
    {

        // metres per pixel, from the crop's physical size in the library
        float metresPerPx = img.referenceImage.size.x / cropSizePx.x;

        Vector2 cropCenterPx = cropPosPx + cropSizePx * 0.5f;
        Vector2 origCenterPx = originalSizePx * 0.5f;
        Vector2 d = (origCenterPx - cropCenterPx) * metresPerPx; // crop center -> original center

        // pixel +x = local +X, pixel +y (down) = local -Z
        Vector3 localOffset = new Vector3(d.x, 0f, -d.y);

        // original panel's center pose in world space
        Vector3 worldPos = img.transform.TransformPoint(localOffset);

        return worldPos;
        //Quaternion worldRot = img.transform.rotation;

        //contentRoot.SetPositionAndRotation(worldPos, worldRot);
    }

}

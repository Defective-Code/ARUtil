using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

[System.Serializable]
public class CropInfo
{
    public string referenceImageName;   // name of the cropped reference image
    public Vector2 originalSizePx;      // W, H of the full panel image
    public Vector2 cropPosPx;           // top-left of crop within original (x, y), y down
    public Vector2 cropSizePx;          // w, h of the crop
    public Transform contentRoot;       // AR content to align with the full panel
}

public class CropAligner : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager manager;
    [SerializeField] List<CropInfo> crops;

    void OnEnable() => manager.trackedImagesChanged += OnChanged;
    void OnDisable() => manager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var img in args.added) Apply(img);
        foreach (var img in args.updated) Apply(img);
    }

    void Apply(ARTrackedImage img)
    {
        if (img.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.None) return;

        var crop = crops.Find(c => c.referenceImageName == img.referenceImage.name);
        if (crop == null) return;

        // metres per pixel, from the crop's physical size in the library
        float metresPerPx = img.referenceImage.size.x / crop.cropSizePx.x;

        Vector2 cropCenterPx = crop.cropPosPx + crop.cropSizePx * 0.5f;
        Vector2 origCenterPx = crop.originalSizePx * 0.5f;
        Vector2 d = (origCenterPx - cropCenterPx) * metresPerPx; // crop center -> original center

        // pixel +x = local +X, pixel +y (down) = local -Z
        Vector3 localOffset = new Vector3(d.x, 0f, -d.y);

        // original panel's center pose in world space
        Vector3 worldPos = img.transform.TransformPoint(localOffset);
        Quaternion worldRot = img.transform.rotation;

        crop.contentRoot.SetPositionAndRotation(worldPos, worldRot);
    }
}
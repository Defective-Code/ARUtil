using Google.XR.ARCoreExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTargetSession : MonoBehaviour
{
    [SerializeField]
    ARAnchorManager aRAnchorManager;

    [SerializeField]
    ARTrackedImageManager aRTrackedImageManager;

    [SerializeField]
    XRReferenceImageLibrary imagesTargets;

    //[SerializeField]
    //GameObject debugPrefab;

    [SerializeField]
    PanelRenderer panelRenderer; // replaces direct Slider/Button references

    Slider ui_StabilizationLoading;
    Button ui_ResetAnchorButton;

    //[SerializeField]
    //GameObject child;

    [SerializeField]
    int se_timeToTrack = 3;

    [SerializeField]
    AnchorData anchorData;

    Dictionary<string, ARAnchor> d_ImageAnchor = new Dictionary<string, ARAnchor>();
    Dictionary<string, float> d_Times = new();
    Dictionary<string, GameObject> d_namesChildren = new();

    string v_currentTracking;

    // Function to reset all the data stuff stored in this class
    public void ClearData()
    {
        d_Times.Clear();
        v_currentTracking = null;
    }

    private void OnValidate()
    {
        // unchanged — no UI Toolkit-specific logic here
        GameObject xrOrigin = GameObject.Find("XR Origin");

        if (aRAnchorManager == null)
        {
            aRAnchorManager = xrOrigin?.GetComponent<ARAnchorManager>();
        }

        if (aRTrackedImageManager == null)
        {
            aRTrackedImageManager = xrOrigin?.GetComponent<ARTrackedImageManager>();
        }

        GameObject imageTargetSession = transform.gameObject;
        if (imagesTargets == null)
        {
            Debug.LogError("Please add a reference image library to this component as well");
        }
        else
        {
            foreach (var referenceImage in imagesTargets)
            {
                Transform foundChild = transform.Find(referenceImage.name);
                GameObject imageObject;
                ImageTarget it;

                if (foundChild != null)
                {
                    imageObject = foundChild.gameObject;
                    it = imageObject.GetComponent<ImageTarget>();
                }
                else
                {
                    imageObject = new GameObject(referenceImage.name);
                    imageObject.transform.SetParent(transform, false);
                    it = imageObject.AddComponent<ImageTarget>();
                    imageObject.AddComponent<MarkerSettings>();
                    var authoringSpace = new GameObject("AuthoringSpace");
                    authoringSpace.transform.SetParent(imageObject.transform, false);
                    authoringSpace.AddComponent<AuthoringSpaceAligner>();
                }

                it.imageWidth = referenceImage.width;
                it.imageHeight = referenceImage.height;
                it.imageTex = referenceImage.texture;
            }
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<MarkerSettings>() == null)
            {
                Debug.LogWarning($"Marker {child.gameObject.name} did not have a MarkerSettings component. Please make sure to add it");
            }
        }
    }

    void Awake()
    {
        // We want to disable all the children representations so they are not visible in the scene
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false); // templates only, never rendered directly
            d_namesChildren.Add(child.name, child.gameObject);
        }
    }

    void OnEnable()
    {
        aRTrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    void OnDisable()
    {
        aRTrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        if (ui_ResetAnchorButton != null)
            ui_ResetAnchorButton.clicked -= ReloadAnchor;
    }

    void OnUIReload(PanelRenderer renderer, VisualElement rootElement, int version)
    {
        ui_StabilizationLoading = rootElement.Q<Slider>("stabilization-loading");
        ui_ResetAnchorButton = rootElement.Q<Button>("reset-anchor-button");

        ui_ResetAnchorButton.clicked += ReloadAnchor;

        // equivalent of ui_ResetAnchorButton.gameObject.SetActive(false)
        ui_ResetAnchorButton.style.display = DisplayStyle.None;
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var updatedImage in eventArgs.updated)
        {
            string imageName = updatedImage.referenceImage.name;

            if (updatedImage.trackingState == TrackingState.Tracking)
            {
                if (!anchorData.ContainsKey(imageName))
                {
                    if (!d_Times.ContainsKey(imageName))
                    {
                        v_currentTracking = imageName;
                        d_Times.Add(imageName, 0f);
                    }
                    else if (v_currentTracking != imageName)
                    {
                        v_currentTracking = imageName;
                        d_Times[imageName] = 0f;
                    }
                    else
                    {
                        if (d_Times[imageName] >= se_timeToTrack)
                        {
                            d_Times[imageName] = 0f;
                            ui_StabilizationLoading.value = 0f;
                            CreateImageAnchor(updatedImage, d_namesChildren[updatedImage.referenceImage.name]);
                        }
                        else
                        {
                            d_Times[imageName] += Time.deltaTime;
                            ui_StabilizationLoading.value = d_Times[imageName] / se_timeToTrack;
                        }
                    }
                }
                else
                {
                    v_currentTracking = updatedImage.referenceImage.name;
                    ui_ResetAnchorButton.style.display = DisplayStyle.Flex; // was .gameObject.SetActive(true)
                }
            }
            else
            {
                if (d_Times.ContainsKey(imageName))
                {
                    d_Times[imageName] = 0f;
                }
            }
        }
    }

    async void CreateImageAnchor(ARTrackedImage image, GameObject templateChild)
    {
        Debug.Log("Creating Image Anchor");

        MarkerSettings childSettings = templateChild.GetComponent<MarkerSettings>();
        bool worldOrient = childSettings.orientRelToAnchor;
        Quaternion flatRotation = Quaternion.Euler(0, image.transform.rotation.eulerAngles.y, 0);
        Pose imagePose = worldOrient ? new Pose(image.transform.position, flatRotation) : new Pose(image.transform.position, image.transform.rotation);

        var result = await aRAnchorManager.TryAddAnchorAsync(imagePose);
        if (result.status.IsSuccess())
        {
            var anchor = result.value;

            //if (debugPrefab != null) Instantiate(debugPrefab, anchor.transform);

            GameObject instance = Instantiate(templateChild, anchor.transform);
            //instance.transform.SetParent(anchor.transform, false);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localPosition = Vector3.zero;
            instance.SetActive(true); // template was disabled; the live clone should be enabled

            Debug.Log($"Image detected at {image.transform.position} and created an anchor at {anchor.transform.position}");
            //Debug.Log($"Child was moved to {child.transform.position}");

            anchorData.AddAnchor(image.referenceImage.name, anchor);
        }
        else
        {
            Debug.LogError($"Failed to create the anchor for tag {image.referenceImage.name} with {result.status}");
        }
    }

    void ReloadAnchor()
    {
        ARAnchor toRemove = anchorData.Get(v_currentTracking);

        if (toRemove == null)
        {
            Debug.Log("toRemove was null, exiting function");
            return;
        }

        //if (toRemove.transform.childCount > 0)
        //{
        //    foreach (Transform child in toRemove.transform)
        //    {
        //        child.transform.SetParent(transform, false);
        //    }
        //}
        DeleteAnchor(toRemove); // this destroys the anchor AND its child together
        anchorData.RemoveAnchor(v_currentTracking);
    }

    void DeleteAnchor(ARAnchor toRemove)
    {
        var result = aRAnchorManager.TryRemoveAnchor(toRemove);

        if (!result)
        {
            Debug.LogError($"Failed to remove anchor");
            return;
        }
    }

    
}
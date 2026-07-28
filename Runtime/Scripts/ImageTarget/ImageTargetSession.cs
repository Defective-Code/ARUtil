using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;

public class ImageTargetSession : MonoBehaviour
{
    //struct GuidCoroutine
    //{
    //    public Guid guid;
    //    public Coroutine c_TrackingTimer; // store the coroutine that is managing the timer to track the image

    //    public GuidCoroutine(Guid guid, Coroutine c_TrackingTimer)
    //    {
    //        this.guid = guid;
    //        this.c_TrackingTimer = c_TrackingTimer;
    //    }

    //    public static bool operator == (GuidCoroutine gc1, GuidCoroutine gc2)
    //    {
    //        return gc1.Equals(gc2);
    //    }

    //    public static bool operator !=(GuidCoroutine gc1, GuidCoroutine gc2)
    //    {
    //        return !gc1.Equals(gc2);
    //    }

    //    public static 
    //}

    [SerializeField]
    ARAnchorManager aRAnchorManager;

    [SerializeField]
    ARTrackedImageManager aRTrackedImageManager;

    [SerializeField]
    XRReferenceImageLibrary imagesTargets;

    [SerializeField]
    GameObject debugPrefab;

    [SerializeField]
    Slider ui_StabilizationLoading;
    [SerializeField]
    Button ui_ResetAnchorButton;

    [SerializeField]
    GameObject child;

    [SerializeField]
    int se_timeToTrack;

    [SerializeField]
    AnchorData anchorData;

    Dictionary<string, ARAnchor> d_ImageAnchor = new Dictionary<string, ARAnchor> ();
    Dictionary<string, float> d_Times = new(); // Dictionary to store the amount of time an image has been tracked.
    Dictionary<string, GameObject> d_namesChildren = new(); // Dictionary to store child gameobjects names and a reference to them for easy use later

    //Guid v_currentTracking; // store the guid of the currently tracking
    string v_currentTracking; // store the name of the currently tracking

    // this function runs in the Unity editor when a value changes or the script is reloaded.
    private void OnValidate()
    {
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
        } else
        {
            foreach (var referenceImage in imagesTargets)
            {
                Transform foundChild = transform.Find(referenceImage.name);
                GameObject imageObject;
                ImageTarget it;

                if (foundChild != null) // if the gameobject already exists, then we just want to modify its imagetarget component and not create a new gameobject
                {
                    imageObject = foundChild.gameObject;
                    it = imageObject.GetComponent<ImageTarget>();
                }
                else
                {
                    imageObject = new GameObject(referenceImage.name); // create an empty gameobject to act as the representative of this imagetarget
                    imageObject.transform.SetParent(transform, false); // parent to this gameobject
                    it = imageObject.AddComponent<ImageTarget>(); // add the imagetarget component to this gameobject for visualization purposes
                    imageObject.AddComponent<MarkerSettings>(); // add the markersettings component for controlling marker behaviour
                    var authoringSpace = new GameObject("AuthoringSpace");
                    authoringSpace.transform.SetParent(imageObject.transform, false);
                    authoringSpace.AddComponent<AuthoringSpaceAligner>();
                }
                   
                it.imageWidth = referenceImage.width;
                it.imageHeight = referenceImage.height;
                it.imageTex = referenceImage.texture;
            }
        }

        // check each child has a MarkerSettings component
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
        foreach(Transform child in transform)
        {
            d_namesChildren.Add(child.name, child.gameObject);
        }

        ui_ResetAnchorButton.gameObject.SetActive(false); // disable the reset anchor button 

        //d_ImageAnchor = anchorData.d_ImageAnchor;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        aRTrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

        ui_ResetAnchorButton.onClick.AddListener(ReloadAnchor);
    }

    void OnDisable()
    {
        aRTrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        ui_ResetAnchorButton.onClick.RemoveListener(ReloadAnchor);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        //foreach (var newImage in eventArgs.added)
        //{
        //    GameObject child = transform.GetChild(0).gameObject;
        //    CreateImageAnchor(newImage, child);
        //}


        foreach (var updatedImage in eventArgs.updated)
        {
            //Guid imageGuid = updatedImage.referenceImage.guid;
            string imageName = updatedImage.referenceImage.name;

            if (updatedImage.trackingState == TrackingState.Tracking)
            {
                //Debug.Log($"Found image : {updatedImage.referenceImage.name}");

                // if we have already created am anchor for this image then we 
                //if (!d_ImageAnchor.ContainsKey(imageGuid))
                if (!anchorData.ContainsKey(imageName))
                {
                    //check if the image has been stable for a period of time, so that the proper position and other locational info has been found correctly.
                    // this condition checks if this is the first time this image has been timed, as it it possible to begin timing an image without successfully adding an anchor
                    //if (!d_Times.ContainsKey(imageGuid))
                    if (!d_Times.ContainsKey(imageName))
                    {
                        //if (v_currentTracking != default(Guid)) d_Times[v_currentTracking] = 0f; // if we had a previously tracking image, then we want to reset the timer for that 

                        //v_currentTracking = imageGuid;
                        v_currentTracking = imageName;
                        //d_Times.Add(imageGuid, 0f); // adding it to the timer dict for the first time
                        d_Times.Add(imageName, 0f); // adding it to the timer dict for the first time
                    }
                    // if the 
                    //else if (v_currentTracking != imageGuid)
                    else if (v_currentTracking != imageName)
                    {
                        //d_Times[v_currentTracking] = 0f; // reset the timer of the previously tracked image

                        //v_currentTracking = imageGuid; // update the image being currently tracked
                        v_currentTracking = imageName; // update the image being currently tracked
                        //d_Times[imageGuid] = 0f;
                        d_Times[imageName] = 0f;

                    }
                    // here we increment the timer, as we have continued to track the same image
                    else
                    {
                        //if (d_Times[imageGuid] >= se_timeToTrack)
                        if (d_Times[imageName] >= se_timeToTrack)
                        {
                            CreateImageAnchor(updatedImage, d_namesChildren[updatedImage.referenceImage.name]); // finding the precomputed gameobject/name pair and using that for our imageanchor
                            ui_StabilizationLoading.value = 0f;
                        }
                        else
                        {
                            //d_Times[imageGuid] += Time.deltaTime;
                            d_Times[imageName] += Time.deltaTime;
                            //ui_StabilizationLoading.value = d_Times[imageGuid] / se_timeToTrack;
                            ui_StabilizationLoading.value = d_Times[imageName] / se_timeToTrack;
                        }
                    }
                }
                // in the case that the image has already been detected and had an anchor generated for it previously.
                else
                {
                    //v_currentTracking = updatedImage.referenceImage.guid; // just so we can reload the anchor if needed
                    v_currentTracking = updatedImage.referenceImage.name; // just so we can reload the anchor if needed
                    ui_ResetAnchorButton.gameObject.SetActive(true); // only the show the reset anchor button when 
                }
            }
            else
            {
                //if (d_Times.ContainsKey(imageGuid))
                if (d_Times.ContainsKey(imageName))
                {
                    //d_Times[imageGuid] = 0f;
                    d_Times[imageName] = 0f;
                    //v_currentTracking = default(Guid);
                }
            }
        }
        
    }

    async void CreateImageAnchor(ARTrackedImage image, GameObject child)
    {

        Debug.Log("Creating Image Anchor");

        //Vector3 worldPosition = Camera.main.transform.TransformPoint(image.transform.position);
        //Quaternion worldRotation = Camera.main.transform.rotation * image.transform.rotation;

        //Pose imagePose = new Pose(worldPosition, worldRotation);
        

        MarkerSettings childSettings = child.GetComponent<MarkerSettings>();
        bool worldOrient = childSettings.orientRelToAnchor; // flag for if we want anchor to spawn in "flat" or to follow orientation of detected image
        Quaternion flatRotation = Quaternion.Euler(0, image.transform.rotation.eulerAngles.y, 0); // create the Quaternion representing the flat orientation
        Pose imagePose = worldOrient ? new Pose(image.transform.position, flatRotation) : new Pose(image.transform.position, image.transform.rotation); // if worldOrient is false that means the anchor follows the oreitnation of the marker


        var result = await aRAnchorManager.TryAddAnchorAsync(imagePose);
        if (result.status.IsSuccess())
        {
            var anchor = result.value;
            
            //var spawned = Instantiate(prefabToSpawn, worldPosition, worldRotation);

            if (debugPrefab != null) Instantiate(debugPrefab, anchor.transform);

            child.transform.SetParent(anchor.transform, false);

            //this is important for resetting the childs relative position to its parent!!! 
            child.transform.localRotation = Quaternion.identity; //ignore x and y rotation to cancel out 
            child.transform.localPosition = Vector3.zero;

            Debug.Log($"Image detected at {image.transform.position} and created an anchor at {anchor.transform.position}");
            Debug.Log($"Child was moved to {child.transform.position}");

            //d_ImageAnchor.Add(image.referenceImage.guid, anchor);
            anchorData.AddAnchor(image.referenceImage.name, anchor);

        }
        else
        {
            Debug.LogError($"Failed to create the anchor for tag {image.referenceImage.name} with {result.status}");

        }

        //ARAnchor anchor = image.gameObject.AddComponent<ARAnchor>();

        //child.transform.SetParent(anchor.transform, false);
        //imageAnchorDict.Add(image.referenceImage.guid, anchor);
        //Debug.Log($"created anchor at {image.transform.position} and moved placeholdr to {child.transform.position}");

    }

    // function to recreate a anchor once the reload button is pressed
    void ReloadAnchor()
    {
        //ARAnchor toRemove = d_ImageAnchor[v_currentTracking];
        ARAnchor toRemove = anchorData.Get(v_currentTracking);

        //toRemove.transform.DetachChildren(); // detach the children of the to be deleted anchor so they are not also destroyed alongside it
        // loop over the anchors children and reparent them to this gameobject if it has children
        if (toRemove.transform.childCount > 0)
        {
            foreach (Transform child in toRemove.transform)
            {
                child.transform.SetParent(transform, false);
            }
        }
        

        DeleteAnchor(toRemove);

        //d_ImageAnchor.Remove(v_currentTracking); // delete the created anchor from the dictionary, meaning on the next update a new one will be created.
        anchorData.RemoveAnchor(v_currentTracking); // delete the created anchor from the dictionary, meaning on the next update a new one will be created.
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

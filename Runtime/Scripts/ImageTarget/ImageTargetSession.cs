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
    //[SerializeField]
    //ARAnchorManager aRAnchorManager;

    //[SerializeField]
    //ARSession session;

    ARTrackedImageManager aRTrackedImageManager;

    [SerializeField]
    XRReferenceImageLibrary imagesTargets;

    //[SerializeField]
    ARSessionHud aRSessionHud;

    //[SerializeField]
    //GameObject debugPrefab;

    //// Unity 6.3: classic UIDocument workflow
    //[SerializeField]
    //UIDocument uiDocument;

    //Slider ui_StabilizationLoading;
    //Button ui_ResetAnchorButton;
    //Label ui_SessionStateLabel;

    //[SerializeField]
    //GameObject child;

    [SerializeField]
    int se_timeToTrack = 3;

    [SerializeField]
    AnchorData anchorData;

    //[SerializeField]
    //ARAnchorOrganizer aRAnchorOrganizer;

    [SerializeField] private bool debug;

    //Dictionary<string, ARAnchor> d_ImageAnchor = new Dictionary<string, ARAnchor>();
    Dictionary<string, float> d_Times = new();
    Dictionary<string, GameObject> d_namesChildren = new();

    string v_currentTracking;

    // Function to reset all the data stuff stored in this class
    public void ClearData()
    {
        d_Times.Clear();
        v_currentTracking = null;
        //ui_ResetAnchorButton.style.display = DisplayStyle.None; // hide the Anchor reset button as we have wiped the state and all detections
    }

    private void OnValidate()
    {
        // unchanged — no UI Toolkit-specific logic here
        //GameObject xrOrigin = GameObject.Find("XR Origin");

        //if (aRAnchorManager == null)
        //{
        //    aRAnchorManager = xrOrigin?.GetComponent<ARAnchorManager>();
        //}

        //if (aRTrackedImageManager == null)
        //{
        //aRTrackedImageManager = xrOrigin?.GetComponent<ARTrackedImageManager>();
        //}

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

    //IEnumerator Start()
    //{

    //    while (ARSession.state < ARSessionState.Ready) yield return null;

    //    // This code disables and reenables the tracked image manager so that the reference library is re-set to the current one
    //    // Because the underlying ARCore / ARKit reference library is set on first start and not updated even when new XRCore gameobejcts contain a new one
    //    aRTrackedImageManager.enabled = false;
    //    yield return null;
    //    aRTrackedImageManager.enabled = true;

    //    session.Reset(); // forces the native session to rebuild with it
    //    while (ARSession.state == ARSessionState.SessionInitializing) yield return null; // wait until the session is actually tracking

    //    if (debug)
    //    {
    //        Debug.LogWarning($"Library: {aRTrackedImageManager.referenceLibrary}, count: {aRTrackedImageManager.referenceLibrary?.count}");
    //        Debug.LogWarning($"Subsystem lib: {aRTrackedImageManager.subsystem?.imageLibrary}");
    //    }
    //}

    private void Start()
    {
        aRSessionHud = (ARSessionHud)UIManager.Instance.GetUIView(UIScreen.ARSessionHud); // get the hud from the singleton uimanager 

        aRTrackedImageManager = ArRoot.Instance.aRTrackedImageManager;
        aRTrackedImageManager.referenceLibrary = imagesTargets; // update the imageTargets refereneLibrary in the trackedImageManager
        aRTrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

        ArRoot.Instance.aRPlayback.SessionReset += ClearData;

        ARSession.stateChanged += OnSessionStateChanged;
    }

    void OnEnable()
    {
        
    }

    private void OnDestroy()
    {
        aRTrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

        ARSession.stateChanged -= OnSessionStateChanged;

        ArRoot.Instance.aRPlayback.SessionReset -= ClearData;
    }

    void OnDisable()
    {

    }


    //// Shared element lookup/binding used by both workflows so the query
    //// logic and button wiring only live in one place.
    //void BindUIElements(VisualElement rootElement)
    //{
    //    ui_StabilizationLoading = rootElement.Q<Slider>("stabilization-loading");
    //    //ui_ResetAnchorButton = rootElement.Q<Button>("reset-anchor-button");
    //    ui_SessionStateLabel = rootElement.Q<Label>("session-label");

    //    //if (ui_ResetAnchorButton != null)
    //    //{
    //    //    ui_ResetAnchorButton.clicked -= ReloadAnchor; // avoid double subscription on rebind
    //    //    ui_ResetAnchorButton.clicked += ReloadAnchor;

    //    //    // equivalent of ui_ResetAnchorButton.gameObject.SetActive(false)
    //    //    ui_ResetAnchorButton.style.display = DisplayStyle.None;
    //    //}
    //}

    void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        //if (ui_SessionStateLabel != null)
        //{
        //    switch (args.state)
        //    {
        //        case ARSessionState.None:
        //            // Device support hasn't been determined yet
        //            break;
        //        case ARSessionState.Unsupported:
        //            // Device doesn't support AR
        //            break;
        //        case ARSessionState.CheckingAvailability:
        //            // Checking if AR is supported
        //            break;
        //        case ARSessionState.NeedsInstall:
        //            // AR software needs to be installed (e.g. ARCore APK)
        //            break;
        //        case ARSessionState.Installing:
        //            // AR software is installing
        //            break;
        //        case ARSessionState.Ready:
        //            // AR is supported and ready, but session hasn't started
        //            break;
        //        case ARSessionState.SessionInitializing:
        //            // Session is starting up, gathering data before tracking begins
        //            break;
        //        case ARSessionState.SessionTracking:
        //            // Session is up and tracking successfully — camera feed should be live here
        //            break;
        //    }
        //    ui_SessionStateLabel.text = ARSession.state.ToString();
        //}

        aRSessionHud.UpdateLabel(ARSession.state.ToString());
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        //foreach (var detectedImage in eventArgs.added)
        //{

        //}

        foreach (var updatedImage in eventArgs.updated)
        {
            string imageName = updatedImage.referenceImage.name;

            //if (debug) Debug.LogWarning($"Detected {imageName}!!!");

            if (updatedImage.trackingState == TrackingState.Tracking)
            {
                if (!anchorData.ContainsKey(imageName))
                {
                    if (!d_Times.ContainsKey(imageName)) // if we dont have an entry for storing the time of tracking for a given image, then create one.
                    {
                        v_currentTracking = imageName;
                        d_Times.Add(imageName, 0f);
                    }
                    else if (v_currentTracking != imageName) // if the image we are now tracking is not what was last tracked and we have already created a timer reocrding for this before, then update the latest tracked image to this one and set the timer back to 0
                    {
                        v_currentTracking = imageName;
                        d_Times[imageName] = 0f;
                    }
                    else 
                    {
                        if (d_Times[imageName] >= se_timeToTrack) // if we have tracked a given image for the min amount of time, then create an achor for that image
                        {
                            d_Times[imageName] = 0f;
                            //ui_StabilizationLoading.value = 0f;
                            aRSessionHud.UpdateSliderValue(0f);
                            CreateImageAnchor(updatedImage, d_namesChildren[updatedImage.referenceImage.name]);
                        }
                        else
                        {
                            d_Times[imageName] += Time.deltaTime;
                            aRSessionHud.UpdateSliderValue(d_Times[imageName] / se_timeToTrack);
                        }
                    }
                }
                else
                {
                    v_currentTracking = imageName;
                    //ui_ResetAnchorButton.style.display = DisplayStyle.Flex; // was .gameObject.SetActive(true)
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

    private void CreateImageAnchor(ARTrackedImage image, GameObject templateChild)
    {
        Debug.Log("Creating Image Anchor");

        MarkerSettings childSettings = templateChild.GetComponent<MarkerSettings>();

        //var result = await aRAnchorManager.TryAddAnchorAsync(imagePose);
        //if (result.status.IsSuccess())
        //{
        //    var anchor = result.value;

        //    //if (debugPrefab != null) Instantiate(debugPrefab, anchor.transform);

        //    GameObject instance = Instantiate(templateChild, anchor.transform);
        //    //instance.transform.SetParent(anchor.transform, false);
        //    instance.transform.localRotation = Quaternion.identity;
        //    instance.transform.localPosition = Vector3.zero;
        //    instance.SetActive(true); // template was disabled; the live clone should be enabled

        //    Debug.Log($"Image detected at {image.transform.position} and created an anchor at {anchor.transform.position}");
        //    //Debug.Log($"Child was moved to {child.transform.position}");

        //    anchorData.AddAnchor(image.referenceImage.name, anchor);
        //}
        //else
        //{
        //    Debug.LogError($"Failed to create the anchor for tag {image.referenceImage.name} with {result.status}");
        //}

        // rotation of the placeholder as authored in the editor
        Quaternion authored = templateChild.transform.rotation;
        Quaternion authoredRef = childSettings.followImageRot ? authored : UprightYaw(templateChild.transform);

        ArRoot.Instance.aRAnchorOrganizer.CreateAnchor(
            image.referenceImage.name,
            childSettings.isCrop ? childSettings.AlignCrop(image) : image.transform.position,
            childSettings.followImageRot ? image.transform.rotation : UprightYaw(image.transform), 
            (ARAnchor anchor) =>
            {
                //if (debugPrefab != null) Instantiate(debugPrefab, anchor.transform);

                GameObject instance = Instantiate(templateChild, anchor.transform);
                //instance.transform.SetParent(anchor.transform, false);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localPosition = Vector3.zero;

                // keep the content in the editor's world axes relative to the real sign
                foreach (var aligner in instance.GetComponentsInChildren<AuthoringSpaceAligner>(true))
                    aligner.transform.localRotation = Quaternion.Inverse(authoredRef);

                instance.SetActive(true); // template was disabled; the live clone should be enabled

                Debug.Log($"Image detected at {image.transform.position} and created an anchor at {anchor.transform.position}");
                //Debug.Log($"Child was moved to {child.transform.position}");
            });
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
        //DeleteAnchor(toRemove); // this destroys the anchor AND its child together
        anchorData.RemoveAnchor(v_currentTracking);
    }

    static Quaternion UprightYaw(Transform imageT)
    {
        Vector3 dir = imageT.up;          // the image's normal (ARCore local +Y)
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)     // image lying flat: use its "top" direction instead
        {
            dir = imageT.forward;
            dir.y = 0f;
        }
        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

}
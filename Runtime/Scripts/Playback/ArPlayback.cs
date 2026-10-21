using System;
using System.Collections;
using System.IO;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;


// Class the wrap up all the recording playback stuff of ARCore Extensions into neater functions that can be called from other classes.
[RequireComponent(typeof(ARPlaybackManager))]
public class ArPlayback : MonoBehaviour
{
    //public static ArPlayback Instance { get; private set; } 


    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaybackManager playbackManager;
    [SerializeField] private GPSPlayback gpsPlayback;
    //[SerializeField] private ARAnchorOrganizer anchorOrganizer;

    // Here add references to each targeting type you added.
    //[SerializeField] private ImageTargetSession imageTargetSession;

    // reference to the UI element that is static and above individual scenes
    private ARPlaybackUIToolkit playbackUI; 

    public bool playingBack; // boolean to track whether or not we are currently playing a recording back
    private ARCoreSessionSubsystem subsystem;

    public event Action SessionReset;
    public event Action<bool> PlaybackStartResult; // fires once we actually know if playback started

    private const float ResetTimeoutSeconds = 5f;

    private string PlaybackFolder => Path.Combine(Application.persistentDataPath, "Recordings");

    private bool transitioning;

    private void Awake()
    {
        //if (Instance != null)
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        //Instance = this;
        //DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(PlaybackFolder))
            Directory.CreateDirectory(PlaybackFolder);

        if (playbackManager == null)
            playbackManager = arSession.GetComponent<ARPlaybackManager>();

        if (playbackManager == null)
            Debug.LogError("ArPlaybackManager: No ARPlaybackManager component found. " +
                            "Assign it in the inspector or ensure it's on the ARSession GameObject.");
    }

    private void Start()
    {
        // Check the permissions for this app
        //PermissionsChecker.CheckPermissions();

        // just setting up the event listeners for the playback UI.
        playbackUI = (ARPlaybackUIToolkit)UIManager.Instance.GetUIView(UIScreen.PlaybackMenu);
        playbackUI.e_PlayPressed += StartPlayback;
        playbackUI.e_StopPressed += StopPlayback;
        playbackUI.e_RefreshList += UpdateRecordingList;
    }

    private void OnDestroy()
    {
        playbackUI.e_PlayPressed -= StartPlayback;
        playbackUI.e_StopPressed -= StopPlayback;
        playbackUI.e_RefreshList -= UpdateRecordingList;
    }

    // Now void, not bool — result comes via PlaybackStartResult since it's async
    public void StartPlayback(string folderName)
    {
        // check for when while already playing back a reocrding the user tries to playback a recording - default this causes a crash
        if (playingBack || transitioning)
        {
            Debug.Log("Already playing back a recording - please stop the currently playing recording before trying to play back a recording | OR currently transitioning from a previous playback");
            return;
        }


        string fullPath = Path.Combine(PlaybackFolder, folderName);
        string recordingPath = Path.Combine(fullPath, "recording.mp4");
        string gpsPath = Path.Combine(fullPath, "gps.json");
        string uri = new Uri(recordingPath).AbsoluteUri;

        Debug.Log($"Checking path: {recordingPath}");
        Debug.Log($"Checking URI: {uri}");
        Debug.Log($"Recording Exists: {File.Exists(recordingPath)}");

        if (!File.Exists(recordingPath))
        {
            Debug.LogError($"Playback file not found: {recordingPath}");
            PlaybackStartResult?.Invoke(false);
            return;
        }

        if (subsystem == null)
        {
            subsystem = arSession.subsystem as ARCoreSessionSubsystem;
            if (subsystem == null)
            {
                Debug.LogError("ArPlaybackManager: ARCoreSessionSubsystem not available. " +
                                "Is the ARSession active and ARCore the active XR loader?");
                PlaybackStartResult?.Invoke(false);
                return;
            }
        }
        ////new Uri(gpsPath).AbsoluteUri
        //gpsPlayback.ReadbackGPSData(gpsPath); // read and load the json gps data into the readback class

        //StartCoroutine(StartPlaybackRoutine(uri));

        //gpsPlayback.StartGPSPlayback(); // start loading the read json data into the LocationData scriptable object

        gpsPlayback.ReadbackGPSData(gpsPath);    // load only
        StartCoroutine(StartPlaybackRoutine(uri));
    }

    private IEnumerator StartPlaybackRoutine(string uri)
    {
        transitioning = true;
        playbackUI.TogglePlayButton(false);
        playbackUI.ToggleStopButton(false);

        ClearSessionState();
        arSession.enabled = false;               // pause the native session
        yield return new WaitForSeconds(0.5f);   // let camera/VIO threads wind down
        yield return WaitForSessionSettled();


        // Reset() is asynchronous — wait until the session has actually left
        // SessionInitializing before touching playback, or we hit the same race again.
        //float elapsed = 0f;
        //while (ARSession.state == ARSessionState.SessionInitializing && elapsed < ResetTimeoutSeconds)
        //{
        //    elapsed += Time.deltaTime;
        //    yield return null;
        //}

        //if (ARSession.state == ARSessionState.SessionInitializing)
        //{
        //    Debug.LogError("ArPlaybackManager: Timed out waiting for session reset before starting playback.");
        //    PlaybackStartResult?.Invoke(false);
        //    yield break;
        //}

        Debug.Log($"Printing filepath : {uri}");
        ArStatus status = subsystem.StartPlaybackUri(uri);
        yield return null;
        arSession.enabled = true;                // resume, now reading the dataset

        if (status != ArStatus.Success)
        {
            Debug.LogError($"Failed to start playback: {status}");
            transitioning = false;
            PlaybackStartResult?.Invoke(false);
            yield break;
        }

        playingBack = true;
        gpsPlayback.StartGPSPlayback();          // only now
        
        transitioning = false;
        playbackUI.TogglePlayButton(true);
        playbackUI.ToggleStopButton(true);

        PlaybackStartResult?.Invoke(true);
    }

    public void UpdateRecordingList()
    {
        playbackUI.UpdateFileList(GetAvailableRecordings());
    }

    public string[] GetAvailableRecordings()
    {
        return Directory.GetDirectories(PlaybackFolder);
    }

    public PlaybackStatus GetCurrentStatus()
    {
        return playbackManager.PlaybackStatus;
    }

    //public void StopPlayback()
    //{
    //    if (!playingBack)
    //    {
    //        Debug.Log("Ignoring stop request as no playback is occuring");
    //        return;
    //    }

    //    subsystem?.StopPlaybackUri();
    //    playingBack = false;

    //    ClearSessionState();
    //}

    public void StopPlayback()
    {
        if (!playingBack || transitioning) return;
        StartCoroutine(StopPlaybackRoutine());
    }

    private IEnumerator StopPlaybackRoutine()
    {
        transitioning = true;
        playbackUI.TogglePlayButton(false);
        playbackUI.ToggleStopButton(false);

        gpsPlayback.StopGPSPlayback();
        subsystem?.StopPlaybackUri();
        playingBack = false;
        ClearSessionState();
        yield return WaitForSessionSettled();
        transitioning = false;

        playbackUI.TogglePlayButton(true);
        playbackUI.ToggleStopButton(true);

    }

    private IEnumerator WaitForSessionSettled()
    {
        yield return null; yield return null;    // let Reset() take effect
        float t = 0f;
        while (ARSession.state == ARSessionState.SessionInitializing && t < ResetTimeoutSeconds)
        { t += Time.deltaTime; yield return null; }
    }

    // method to clear all the data that was created during the playback
    private void ClearSessionState()
    {
        //anchorOrganizer.ClearAnchors(); // 
        //imageTargetSession.ClearData();
        //arSession.Reset();
        SessionReset?.Invoke();

    }
}
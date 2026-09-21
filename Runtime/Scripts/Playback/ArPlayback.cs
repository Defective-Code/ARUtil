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
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARPlaybackManager playbackManager;
    [SerializeField] private GPSPlayback gpsPlayback;
    [SerializeField] private ARAnchorOrganizer anchorOrganizer;

    // Here add references to each targeting type you added.
    [SerializeField] private ImageTargetSession imageTargetSession;
     
    private bool playingBack;
    private ARCoreSessionSubsystem subsystem;

    public event Action SessionReset;
    public event Action<bool> PlaybackStartResult; // fires once we actually know if playback started

    private const float ResetTimeoutSeconds = 5f;

    private string PlaybackFolder => Path.Combine(Application.persistentDataPath, "Recordings");

    private void Awake()
    {
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
    }

    // Now void, not bool — result comes via PlaybackStartResult since it's async
    public void StartPlayback(string folderName)
    {
        // check for when while already playing back a reocrding the user tries to playback a recording - default this causes a crash
        if (playingBack)
        {
            Debug.Log("Already playing back a recording - please stop the currently playing recording before trying to play back a recording");
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
        //new Uri(gpsPath).AbsoluteUri
        gpsPlayback.ReadbackGPSData(gpsPath); // read and load the json gps data into the readback class

        StartCoroutine(StartPlaybackRoutine(uri));

        gpsPlayback.StartGPSPlayback(); // start loading the read json data into the LocationData scriptable object
    }

    private IEnumerator StartPlaybackRoutine(string uri)
    {
        ClearSessionState(); // arSession.Reset() + SessionReset event

        // Reset() is asynchronous — wait until the session has actually left
        // SessionInitializing before touching playback, or we hit the same race again.
        float elapsed = 0f;
        while (ARSession.state == ARSessionState.SessionInitializing && elapsed < ResetTimeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ARSession.state == ARSessionState.SessionInitializing)
        {
            Debug.LogError("ArPlaybackManager: Timed out waiting for session reset before starting playback.");
            PlaybackStartResult?.Invoke(false);
            yield break;
        }

        Debug.Log($"Printing filepath : {uri}");
        ArStatus status = subsystem.StartPlaybackUri(uri);
        if (status != ArStatus.Success)
        {
            Debug.LogError($"Failed to start playback: {status}");
            PlaybackStartResult?.Invoke(false);
            yield break;
        }

        playingBack = true;
        PlaybackStartResult?.Invoke(true);
    }

    public string[] GetAvailableRecordings()
    {
        return Directory.GetDirectories(PlaybackFolder);
    }

    public PlaybackStatus GetCurrentStatus()
    {
        return playbackManager.PlaybackStatus;
    }

    public void StopPlayback()
    {
        if (!playingBack)
        {
            Debug.Log("Ignoring stop request as no playback is occuring");
            return;
        }

        subsystem?.StopPlaybackUri();
        playingBack = false;

        ClearSessionState();
    }

    // method to clear all the data that was created during the playback
    private void ClearSessionState()
    {
        arSession.Reset();
        //SessionReset?.Invoke();
        anchorOrganizer.ClearAnchors(); // 
        imageTargetSession.ClearData();
    }
}
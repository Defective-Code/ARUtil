using System.Collections.Generic;
using System.IO;
using System;

using UnityEngine;
using System.Collections;

[System.Serializable]
public class GpsSample
{
    public double timestampUnix;      // seconds since epoch, matches video/recording timeline
    public float sessionTimeSeconds;  // seconds since recording started (easier to sync with video)
    public float latitude;
    public float longitude;
    public float altitude;
}

[System.Serializable]
public class GpsRecordingData
{
    public string recordingId;
    public List<GpsSample> samples = new List<GpsSample>();
}

public class GPSPlayback : MonoBehaviour
{

    [SerializeField] private LocationData locationData; // scriptableobject for holding the current GPS position. This will be overidden if we are playing a recording back and there is an applicaable position json file.

    private GpsRecordingData gpsRecordingData; // class to store the timestamps of GPS in. 
    private int dataIndex; // index of which data entry we are currently at
    private int interval; // interval between gps data recordings.

    void Start() 
    {
        if (locationData == null)
        {
            Debug.LogError("LocationData ScriptableObject was null. Please create an instance of it inside your assets folder and set a reference to in in the inspector.");
            return;
        }

        if (RetrieveLocationData.Instance == null)
        {
            Debug.LogError("LocationService was null, meaning in this scene doesn't use a GPS feature / does not include the relevant component. Therefore we can ignore any GPS playback");
            this.enabled = false; // disable this component if there is no GPS feature in this scene.
            return;
        }
    }

    // Class to read from a file and convert the json back into a data structure
    public void ReadbackGPSData(string filepath)
    {
        if (!File.Exists(filepath))
        {
            Debug.LogError($"GPS data JSON file did not exist at {filepath}");
            return;
        }

        if (!filepath.EndsWith(".json"))
        {
            Debug.LogError($"The filepath provided does not point to a JSON file");
            return;
        }

        string json = File.ReadAllText(filepath); // read the text in the json file

        gpsRecordingData = JsonUtility.FromJson<GpsRecordingData>(json); // de-serialize the json string back into the class for parsing

        if (gpsRecordingData == null || gpsRecordingData.samples == null || gpsRecordingData.samples.Count == 0)
        {
            Debug.LogError($"GPS JSON at {filepath} parsed to null/empty. Raw content length: {json.Length}");
            return;
        }

        Debug.Log($"GPS data loaded: {gpsRecordingData.samples.Count} samples");
        interval = GetSampleInterval();
    }

    // Returns a floored int interval value
    int GetSampleInterval()
    {
        if (gpsRecordingData == null)
        {
            Debug.LogError("GetSampleInterval was called before gpsRecordingData was populated");
            return -1;
        }

        return (int)Math.Floor(gpsRecordingData.samples[1].sessionTimeSeconds - gpsRecordingData.samples[0].sessionTimeSeconds);
    }

    public void StartGPSPlayback()
    {
        if (gpsRecordingData == null)
        {
            Debug.LogError("gpsRecordingData was NULL, please call ReadbackGPSData first with a valid filepath");
            return;
        }

         StartCoroutine(PollGPSData());
    }

    // Coroutine to poll the GPS data on the correct intervals
    IEnumerator PollGPSData()
    {
        if (gpsRecordingData == null || gpsRecordingData.samples == null || gpsRecordingData.samples.Count == 0)
        {
            Debug.LogError("PollGPSData started without valid GPS recording data. Call ReadbackGPSData first.");
            yield break;
        }

        // If our scene we want to playback in is actively retrieving the gps information, we want to temporariliy stop it while we playback
        RetrieveLocationData locationDataService = RetrieveLocationData.Instance;
        if (locationDataService != null)
        {
            locationDataService.StopService();
        }
        else
        {
            Debug.LogWarning("LocationService instance was null so readback of GPS information was not successful.");
            yield break; // get out of coroutine as there is no active lcoation readback.
        }

        while (dataIndex < gpsRecordingData.samples.Count)
        {
            GpsSample sample = gpsRecordingData.samples[dataIndex];

            if (locationData == null)
            {
                Debug.LogError("PollGPSData: locationData ScriptableObject reference is null.");
                yield break;
            }

            locationData.latitude = sample.latitude;
            locationData.longitude = sample.longitude;
            locationData.altitude = sample.altitude;

            if (locationData.locationDataUpdated == null)
            {
                Debug.LogWarning("PollGPSData: locationData.locationDataUpdated UnityEvent is null OR simply has no subscribers within the scene (meaning no function is being exectued when this Action is invoked).");
                //yield break;
            }
            locationData.locationDataUpdated?.Invoke(); // check that the delegate has at least one subscribed function before invoking - otherwise causes a crash

            yield return new WaitForSeconds(interval);
            dataIndex++;
        }

        // Restart the retreival of gps info if the app was doing it prior to playback.
        if (locationDataService != null)
        {
            locationDataService.StartService();
        }
    }
}

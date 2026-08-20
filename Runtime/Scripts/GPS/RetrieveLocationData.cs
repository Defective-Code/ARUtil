using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RetrieveLocationData : MonoBehaviour
{
    public static RetrieveLocationData Instance { set; get; }

    private Coroutine locationService;

    public LocationData locationData; //Scriptable object for storing the position information

    private int counter;

    private void Awake()
    {

        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject); // prevent duplicate creations of this gameObject
        }
        
    }

    private void OnEnable()
    {   
        StartService();
    }

    private void OnDisable()
    {
        StopService();
    }

    public void StartService()
    {
        locationService = StartCoroutine(StartLocationService());
    }

    public void StopService()
    {
        if (locationService == null)
        {
            Debug.LogError("LocationService coroutine is currently not running, nothing to stop. Check the logs to make sure the coroutine has started correctly");
        }

        StopCoroutine(locationService);
    }

    private IEnumerator StartLocationService()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("GPS not enabled by user, please allow the use of location permission");
            yield break;
        }

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait <= 0)
        {
            Debug.LogError("Timed out : Service didn't start in 20 seconds");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            yield break;
        }

        // Set locational infomations
        while (true)
        {
            counter = 0; //reset the 10 second timer
            locationData.latitude = Input.location.lastData.latitude;
            locationData.longitude = Input.location.lastData.longitude;
            locationData.altitude = Input.location.lastData.altitude;
            while (counter < 10)
            {
                yield return new WaitForSeconds(1);
                counter++;
                //if (text) GPSText.text = $"{latitude}, {longitude}, {altitude}";
            }

            locationData.locationDataUpdated?.Invoke(); // fire location updated event for any listeners after updating the locational info
        }
    }
}
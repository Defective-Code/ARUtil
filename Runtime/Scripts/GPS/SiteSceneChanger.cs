using System;
using System.Collections;
using UnityEngine;


public static class GeoUtil
{

    const double EarthRadius = 6371000.0; // meters
    // inputs assumed to be in radians
    public static double DistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        double dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                   System.Math.Cos(lat1 * Mathf.Deg2Rad) * System.Math.Cos(lat2 * Mathf.Deg2Rad) *
                   System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
        return EarthRadius * 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
    }

    public static double Haversine(double lat1, double lat2, double lon1,  double lon2)
    {
        var sdlat = Math.Sin((lat2 - lat1) * Mathf.Deg2Rad / 2);
        var sdlon = Math.Sin((lon2 - lon1) * Mathf.Deg2Rad / 2);
        var q = sdlat * sdlat + Math.Cos(lat1 * Mathf.Deg2Rad) * Math.Cos(lat2 * Mathf.Deg2Rad) * sdlon * sdlon;
        var d = 2 * EarthRadius * Math.Asin(Math.Sqrt(q));

        return d;
    }
}

public class SiteSceneChanger : MonoBehaviour
{
    [SerializeField] private SiteData siteData;
    [SerializeField] private LocationData locationData;

    public float enterRadius = 75f;   // metres
    public float exitRadius = 100f;   // larger than enter, to avoid flickering
    public float maxAccuracy = 50f;   // ignore readings worse than this

    private int currentSiteIndex = -1; // store the index of the sign within the radius 

    void Awake()
    {
        if (locationData == null)
        {
            Debug.LogError("Location data was null : check the inspector and make sure you have set it to an instance of LocationData");
            return;
        }
    }

    private void OnEnable()
    {
        if (locationData == null) return;

        locationData.locationDataUpdated += CheckForSitesNearby;
    }

    private void OnDisable()
    {
        if (locationData != null)
        {
            locationData.locationDataUpdated -= CheckForSitesNearby;
        }
    }

    /// <summary>
    /// Function to check if any sites are nearby to the current position
    /// </summary>
    private void CheckForSitesNearby()
    {
        //Vector2 currentPos = new Vector2(locationData.latitude, locationData.longitude);

        // Check that we have already been near a site before checking we are still in a radius
        if (currentSiteIndex != -1)
        {
            var currentSite = siteData.sites[currentSiteIndex];

            // Still inside the current sign's zone?
            if (GeoUtil.Haversine(locationData.latitude, currentSite.lat, locationData.longitude, currentSite.lon) <= exitRadius) // if a site is currently stored and the distance between our position and the site is within the exit distance then we are still within the range of the site
                return;
   
        }

        StartCoroutine(CheckSitesCoroutine());

    }

    IEnumerator CheckSitesCoroutine()
    {
        // Otherwise find the nearest sign within the enter radius
        currentSiteIndex = -1;
        double best = enterRadius;
        for (int i = 0; i < siteData.sites.Count; i++)
        {
            var site = siteData.sites[i];
            double dist = GeoUtil.Haversine(locationData.latitude, site.lat, locationData.longitude, site.lon);
            if (dist <= best) 
            { 
                best = dist; 
                currentSiteIndex = i; 
                break; // if we have found a sign within the radius, we can break as it is not possible to have more than one sign within the radius
            }
            yield return null;
        }

        if (currentSiteIndex != -1)
        {

            var bestSite = siteData.sites[currentSiteIndex];
            SceneSwitcher.Instance.SwitchScene(bestSite.name); // here switch the scene if we found a site within range
        }
    }
}

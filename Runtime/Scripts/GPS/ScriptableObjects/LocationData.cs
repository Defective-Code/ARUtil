using UnityEngine;
using System;

[CreateAssetMenu(fileName = "LocationData", menuName = "Scriptable Objects/LocationData")]
public class LocationData : ScriptableObject
{
    public float latitude;
    public float longitude;
    public float altitude;

    public Action locationDataUpdated;
}

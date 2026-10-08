using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SiteData", menuName = "Scriptable Objects/SiteData")]
public class SiteData : ScriptableObject
{
    //public SerializableDictionary sites = new SerializableDictionary();   
    [SerializeField] public List<Site> sites = new List<Site>() {
        new Site(0, "Pohutakawa", -46.59798f, 168.3364f),
        new Site(1, "Tupuanuku", -45.98394f, 168.7997f)
    };

    [Serializable]
    public struct Site
    {
        public int id;
        public string name;
        public float lat;
        public float lon;

        public Site(int id, string name, float lat, float lon)
        {
            this.id = id;
            this.name = name;
            this.lat = lat;
            this.lon = lon;
        }
    }
}

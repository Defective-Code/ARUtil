using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class SerializableDictionary
{
    [Serializable]
    public class Item
    {
        public int key;
        public GameObject value;
    }

    [SerializeField]
    private List<Item> items = new();

    private Dictionary<int, GameObject> dictionary;

    public Dictionary<int, GameObject> Dictionary
    {
        get
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<int, GameObject>();

                foreach (var item in items)
                {
                    dictionary[item.key] = item.value;
                }
            }

            return dictionary;
        }
    }

    public GameObject Get(int key)
    {
        Dictionary.TryGetValue(key, out var value);
        return value;
    }

    public bool Contains(int key)
    {
        return Dictionary.ContainsKey(key);
    }

    public System.Collections.Generic.Dictionary<int, UnityEngine.GameObject>.KeyCollection GetKeys()
    {
        return Dictionary.Keys;
    }
}
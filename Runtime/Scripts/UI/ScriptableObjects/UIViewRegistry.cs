using UnityEngine;
using UnityEngine.UIElements;

using System;

[CreateAssetMenu(fileName = "UIViewRegistry", menuName = "Scriptable Objects/UIViewRegistry")]
public class UIViewRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public UIScreen screen;
        public VisualTreeAsset uxml;
    }

    public Entry[] entries;

    public VisualTreeAsset Get(UIScreen screen)
    {
        foreach (var e in entries)
        {
            if (e.screen == screen)
                return e.uxml;
        }
        return null;
    }
}

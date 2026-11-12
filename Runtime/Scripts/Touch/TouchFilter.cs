using System;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public static class TouchFilter
{
    public static bool debug;

    /// Fraction of the screen's longer side that is blocked along every edge.
    public static float edgeFraction = 0.03f;

    /// True if the touch STARTED somewhere acceptable.
    public static bool IsValid(Touch touch) => IsValidPosition(touch.startScreenPosition);

    /// True if a touch beginning at this position (screen pixels, origin bottom-left) is acceptable.
    public static bool IsValidPosition(Vector2 screenPos)
    {
        float margin = edgeFraction * Mathf.Max(Screen.width, Screen.height);

        bool valid =
            screenPos.x >= margin && screenPos.x <= Screen.width - margin &&
            screenPos.y >= margin && screenPos.y <= Screen.height - margin;

        if (debug)
            Debug.Log($"TouchFilter: {screenPos} on {Screen.width}x{Screen.height}, margin {margin:F0}px -> {(valid ? "valid" : "rejected")}");

        return valid;
    }
}
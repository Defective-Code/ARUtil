using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ArRoot : MonoBehaviour
{
    public static ArRoot Instance;

    public ARTrackedImageManager aRTrackedImageManager;
    public ArPlayback aRPlayback;
    public ARAnchorOrganizer aRAnchorOrganizer;
    public ARSession aRSession;

    void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Method to reset the AR session when needed
    /// </summary>
    public void ResetArSession()
    {
        aRAnchorOrganizer.ClearAnchors();
        aRSession.Reset();
    }
}

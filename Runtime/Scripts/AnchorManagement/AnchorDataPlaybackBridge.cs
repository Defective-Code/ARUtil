using UnityEngine;

// This class is for resetting 
public class AnchorDataPlaybackBridge : MonoBehaviour
{
    [SerializeField] private ArPlayback arPlayback;
    [SerializeField] private AnchorData anchorData;
    [SerializeField] private ImageTargetSession imageTargetSession;

    void OnEnable()
    {
        arPlayback.SessionReset += HandleSessionReset;
    }

    void OnDisable()
    {
        arPlayback.SessionReset -= HandleSessionReset;    
    }

    void HandleSessionReset()
    {
        anchorData.ResetAnchors();
        
        // Clear data for each non-null image target session
        imageTargetSession?.ClearData();
    }
}

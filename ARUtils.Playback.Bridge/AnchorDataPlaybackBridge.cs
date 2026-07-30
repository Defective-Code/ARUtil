using UnityEngine;

public class AnchorDataPlaybackBridge : MonoBehaviour
{
    [SerializeField] private ArPlaybackManager arPlaybackManager;
    [SerializeField] private AnchorData anchorData;
    [SerializeField] private ImageTargetSession targetSession;

    void OnEnable()
    {
        arPlaybackManager.SessionReset += HandleSessionReset;
    }

    void OnDisable()
    {
        arPlaybackManager.SessionReset -= HandleSessionReset;    
    }

    void HandleSessionReset()
    {
        anchorData.ResetAnchors();
        targetSession.ClearData();
    }
}

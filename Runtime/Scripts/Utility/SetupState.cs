using UnityEngine;

/// <summary>
/// Class to setup the basic stuff for the application that needs to be done from code, such as framerate or override default behaviours
/// </summary>
public class SetupState : MonoBehaviour
{
    [Header ("Frame Rate")]
    [SerializeField] private int frameRate = 30;

    void Start()
    {
        Application.targetFrameRate = frameRate;

        Input.backButtonLeavesApp = false; // disable default behaviour of the back button
    }

}

using UnityEngine;
using UnityEngine.SceneManagement;

// Class that switches to different scenes depending on the position of the device
public class GeofenceManager : MonoBehaviour
{

    public static GeofenceManager Instance;

    public LocationData locationData;

    private Vector2 owheoLatLong = new Vector2(-45.867000f, 170.518167f);
    private Vector2 polytechLatLong = new Vector2(-45.866222f, 170.518861f);

    private string currentScene = null;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the scene name 
        if (currentScene == null)
        {
            currentScene = SceneManager.GetActiveScene().name;
        }
        

        if (locationData == null)
        {
            Debug.LogError("Location data was null : check the inspector and make sure you have set it to an instance of LocationData");
            return;
        }
        locationData.locationDataUpdated += CheckFence; // subscribe to the updated location event

        SceneManager.activeSceneChanged += UpdateScene; // update the scene value whenever a scene is laoded

    }

    // Update checks which "real" scene is closer, and then switches to the appropriate scene
    public void CheckFence()
    {   

        if (locationData == null)
        {
            Debug.LogWarning("Geofencing is disabled due to locationData not being set in the editor");
            return;
        }

        var latitude = locationData.latitude;
        var longitude = locationData.longitude;
        var altitude = locationData.altitude;

        // if the lat or long are the default values, then we know we haven't updated yet
        if (latitude == 0 || longitude == 0)
        {
            return;
        }

        bool closer = CloserScene(new Vector2(latitude, longitude));
        var scene = (closer ? "Polytech" : "Owheo");

        if (scene == currentScene) return; // if the scene is the same as the one we are in right now, then don't switch

        SceneSwitcher.Instance.SwitchScene(scene); // switch to the scene that is closer
    }

    bool CloserScene(Vector2 device)
    {
        float distOwheo = (device -  owheoLatLong).sqrMagnitude;
        float distPolytech = (device -  polytechLatLong).sqrMagnitude;

        return (distOwheo >  distPolytech); // returns true for polytech being closer, false for owheo being closer
    }

    void UpdateScene(Scene current, Scene next)
    {
        currentScene = next.name;
    }
}

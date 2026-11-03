using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapMarkers : MonoBehaviour
{
    [SerializeField] private SiteData siteData;

    private TileMapPanel tileMapPanel;

    [Tooltip("Debug flag - enable to add the option to change the current location to the tapped marker position")]
    [HideInInspector] 
    public bool debug;

    [HideInInspector]
    public LocationData locationData;

    private void Awake()
    {
        tileMapPanel = GetComponent<TileMapPanel>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var site in siteData.sites)
        {
            tileMapPanel.AddMarker(site.name, site.lat, site.lon);
            //tileMapPanel.AddMarker("Kahuariki", site.lat, site.lon); // temp change for demo purpose
        }

        tileMapPanel.MarkerClicked += MarkerClickedListener;
    }

    private void MarkerClickedListener(string id, double lat, double lon)
    {
        Debug.Log($"MapMarkers debug = {debug}, locationData = {locationData}", this);

        VisualElement markerContent = new VisualElement();
        markerContent.style.flexGrow = 0;
        markerContent.style.flexShrink = 0;
        markerContent.style.alignSelf = Align.Center;
        markerContent.style.flexDirection = FlexDirection.Column;
        markerContent.style.color = Color.white;

        // Optional: make it read as a rectangle/panel
        markerContent.style.paddingTop = 10;
        markerContent.style.paddingBottom = 10;
        markerContent.style.paddingLeft = 10;
        markerContent.style.paddingRight = 10;
        markerContent.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        markerContent.style.borderTopLeftRadius = 8;
        markerContent.style.borderTopRightRadius = 8;
        markerContent.style.borderBottomLeftRadius = 8;
        markerContent.style.borderBottomRightRadius = 8;



        Label switchLabel = new Label { text = $"Switch scene to {id}" };
        switchLabel.style.color = Color.white;

        Button cancelButton = new Button { text = "Cancel" };
        cancelButton.clicked += () => {
            UIManager.Instance.Pop();  //this pops the popup off
        };

        Button acceptButton = new Button { text = "Accept" };
        acceptButton.clicked += () =>
        {
            UIManager.Instance.Pop(); //this pops the popup off

            SceneSwitcher.Instance.SwitchScene(id);
            //SceneSwitcher.Instance.SwitchScene("Kahuariki"); //temp change for demo
        };

        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;

        buttonContainer.Add(cancelButton);
        buttonContainer.Add(acceptButton);

        markerContent.Add(switchLabel);
        markerContent.Add(buttonContainer);

        if (debug)
        {
            Button changeGPSPosition = new Button { text = "Change GPS position" };
            changeGPSPosition.clicked += () =>
            {
                RetrieveLocationData.Instance.StopService(); // stop retreving the data live

                locationData.latitude = (float)lat;
                locationData.longitude = (float)lon;
                locationData.locationDataUpdated?.Invoke(); // call the event that shows location has changed
            };

            markerContent.Add(changeGPSPosition);
        }

        UIManager.Instance.Push(UIScreen.Popup, markerContent);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MapMarkers))]
public class MapMarkersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // siteData etc.

        serializedObject.Update();

        var debugProp = serializedObject.FindProperty("debug");
        EditorGUILayout.PropertyField(debugProp, new GUIContent("Debug flag"));

        if (debugProp.boolValue)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("locationData"),
                new GUIContent("Location Data"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
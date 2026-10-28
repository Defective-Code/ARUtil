using UnityEngine;
using UnityEngine.UIElements;

public class MapMarkers : MonoBehaviour
{
    [SerializeField] private SiteData siteData;

    private TileMapPanel tileMapPanel;

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

    private void MarkerClickedListener(string id)
    {
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

        UIManager.Instance.Push(UIScreen.Popup, markerContent);
    }
}

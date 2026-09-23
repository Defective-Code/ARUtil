using UnityEngine;
using UnityEngine.UIElements;

public class ARSessionHud : UIViewBehaviour
{

    Slider ui_StabilizationLoading;
    Label ui_SessionStateLabel;

    // Nav menu
    Button ui_ToggleNavButton;
    VisualElement ui_NavMenu;
    Button ui_OpenAnchorMenu;
    Button ui_OpenPlaybackMenu;

    bool navOpen; 

    //private void OnEnable()
    protected override void OnInitialize()
    {

        BindUIElements(Root);
    }

    // Shared element lookup/binding used by both workflows so the query
    // logic and button wiring only live in one place.
    void BindUIElements(VisualElement rootElement)
    {
        ui_StabilizationLoading = rootElement.Q<Slider>("stabilization-loading");
        //ui_ResetAnchorButton = rootElement.Q<Button>("reset-anchor-button");
        ui_SessionStateLabel = rootElement.Q<Label>("session-label");
        
        ui_ToggleNavButton = rootElement.Q<Button>("ar-toggle-dropdown");
        ui_NavMenu = rootElement.Q<VisualElement>("ar-menu-dropdown");
        ui_OpenAnchorMenu = rootElement.Q<Button>("anchor-menu-button");
        ui_OpenPlaybackMenu = rootElement.Q<Button>("playback-menu-button");

        ui_ToggleNavButton.clicked += () =>
        {
            Debug.Log("Clicked Toggle Nav");
            navOpen = !navOpen;
            ui_NavMenu.style.display = navOpen ? DisplayStyle.Flex : DisplayStyle.None;
            ui_ToggleNavButton.text = navOpen ? "Close" : "Open";
        };

        ui_OpenAnchorMenu.clicked += () =>
        {
            Debug.Log("Clicked Anchor Menu button");
            UIManager.Instance.Push(UIScreen.AnchorMenu);
        };

        ui_OpenPlaybackMenu.clicked += () =>
        {
            Debug.Log("Clicked Playback Menu button");
            UIManager.Instance.Push(UIScreen.PlaybackMenu);
        };
    }

    public void UpdateLabel(string text)
    {
        if (ui_SessionStateLabel == null)
        {
            Debug.LogError("Session Label was null in the hud");
            return;
        }

        ui_SessionStateLabel.text = text;
    }

    public void UpdateSliderValue(float value)
    {
        ui_StabilizationLoading.value = value;
    }
}

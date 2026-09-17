#if ARPlayback_Unity_6_5

using UnityEngine;
using UnityEngine.UIElements;

// partial class for editor version 6.5
[RequireComponent(typeof(PanelRenderer))]
public partial class ARPlaybackUIToolkit
{
    private PanelRenderer panelRenderer;

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable()
    {
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        if (recordingsList != null)
            recordingsList.selectionChanged -= OnSelectionChanged;
        if (playButton != null) playButton.clicked -= OnPlayPressed;
        if (stopButton != null) stopButton.clicked -= OnStopPressed;
        if (refreshButton != null) refreshButton.clicked -= RefreshList;
    }
    
    private void OnUIReload(PanelRenderer renderer, VisualElement root)
    {
        //var rootVisualElement = root.Q<VisualElement>("root-container");
        root.pickingMode = PickingMode.Ignore; // disable picking up pointer events on the root visual element so we only capture events we want to capture
        
        var toggleButton = root.Q<Button>("toggle-panel-button");
        toggleButton.pickingMode = PickingMode.Position;
        toggleButton.clicked += TogglePanel;

        playbackPanel = root.Q<VisualElement>("playback-panel");
        playbackPanel.pickingMode = PickingMode.Position; // enable picking up pointer events on this parent VisualElement so the buttons work correctly.

        recordingsList = root.Q<ListView>("recordings-list");
        statusLabel = root.Q<Label>("status-label");
        selectedLabel = root.Q<Label>("selected-label");
        playButton = root.Q<Button>("play-button");
        stopButton = root.Q<Button>("stop-button");
        refreshButton = root.Q<Button>("refresh-button");

        recordingsList.makeItem = () => new Label();
        recordingsList.bindItem = (element, i) => (element as Label).text = fileNames[i];
        recordingsList.fixedItemHeight = 32;
        recordingsList.selectionType = SelectionType.Single;
        recordingsList.itemsSource = fileNames;
        recordingsList.selectionChanged += OnSelectionChanged;

        playButton.clicked += OnPlayPressed;
        stopButton.clicked += OnStopPressed;
        refreshButton.clicked += RefreshList;

        playButton.SetEnabled(false);

        RefreshList();
    }
}

#endif
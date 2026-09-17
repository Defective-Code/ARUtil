#if ARPlayback_Unity_6_3

using UnityEngine;
using UnityEngine.UIElements;

// partial class for editor version 6.3
[RequireComponent(typeof(UIDocument))]
public partial class ARPlaybackUIToolkit
{
    private UIDocument uiDocument;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // UIDocument's rootVisualElement is available immediately once the document
        // is active, so we bind once here instead of using a reload callback.
        BindUI(uiDocument.rootVisualElement);
    }

    private void OnDisable()
    {
        if (recordingsList != null)
            recordingsList.selectionChanged -= OnSelectionChanged;
        if (playButton != null) playButton.clicked -= OnPlayPressed;
        if (stopButton != null) stopButton.clicked -= OnStopPressed;
        if (refreshButton != null) refreshButton.clicked -= RefreshList;

        var toggleButton = uiDocument.rootVisualElement?.Q<Button>("toggle-panel-button");
        if (toggleButton != null) toggleButton.clicked -= TogglePanel;
    }

    // Called once after the UIDocument's visual tree is built and attached
    private void BindUI(VisualElement root)
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
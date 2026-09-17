using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Class to manage the UI elements and perform the relevant actions (when playback button pressed playback the relevant scene etc)
public partial class ARPlaybackUIToolkit : MonoBehaviour
{
    [SerializeField] private ArPlayback arPlayback;
    //public UnityEngine.UI.Button toggleButton;

    //private PanelRenderer panelRenderer;
    private ListView recordingsList;
    private Label statusLabel;
    private Label selectedLabel;
    private Button playButton;
    private Button stopButton;
    private Button refreshButton;
    private VisualElement playbackPanel;

    private bool isVisible = true;

    private List<string> fileNames = new List<string>();
    private string selectedFile;

    //private void Awake()
    //{
    //    panelRenderer = GetComponent<PanelRenderer>();
    //}

    //private void OnEnable()
    //{
    //    panelRenderer.RegisterUIReloadCallback(OnUIReload);
    //}

    //private void OnDisable()
    //{
    //    panelRenderer.UnregisterUIReloadCallback(OnUIReload);

    //    if (recordingsList != null)
    //        recordingsList.selectionChanged -= OnSelectionChanged;
    //    if (playButton != null) playButton.clicked -= OnPlayPressed;
    //    if (stopButton != null) stopButton.clicked -= OnStopPressed;
    //    if (refreshButton != null) refreshButton.clicked -= RefreshList;
    //}

    // Called once the visual tree from the assigned UXML is built and attached
    //private void OnUIReload(PanelRenderer renderer, VisualElement root)
    //{
    //    //var rootVisualElement = root.Q<VisualElement>("root-container");
    //    root.pickingMode = PickingMode.Ignore; // disable picking up pointer events on the root visual element so we only capture events we want to capture
        
    //    var toggleButton = root.Q<Button>("toggle-panel-button");
    //    toggleButton.pickingMode = PickingMode.Position;
    //    toggleButton.clicked += TogglePanel;

    //    playbackPanel = root.Q<VisualElement>("playback-panel");
    //    playbackPanel.pickingMode = PickingMode.Position; // enable picking up pointer events on this parent VisualElement so the buttons work correctly.

    //    recordingsList = root.Q<ListView>("recordings-list");
    //    statusLabel = root.Q<Label>("status-label");
    //    selectedLabel = root.Q<Label>("selected-label");
    //    playButton = root.Q<Button>("play-button");
    //    stopButton = root.Q<Button>("stop-button");
    //    refreshButton = root.Q<Button>("refresh-button");

    //    recordingsList.makeItem = () => new Label();
    //    recordingsList.bindItem = (element, i) => (element as Label).text = fileNames[i];
    //    recordingsList.fixedItemHeight = 32;
    //    recordingsList.selectionType = SelectionType.Single;
    //    recordingsList.itemsSource = fileNames;
    //    recordingsList.selectionChanged += OnSelectionChanged;

    //    playButton.clicked += OnPlayPressed;
    //    stopButton.clicked += OnStopPressed;
    //    refreshButton.clicked += RefreshList;

    //    playButton.SetEnabled(false);

    //    RefreshList();
    //}

    private void RefreshList()
    {
        fileNames.Clear();
        foreach (string path in arPlayback.GetAvailableRecordings())
            fileNames.Add(Path.GetFileName(path));

        recordingsList.itemsSource = fileNames;
        recordingsList.Rebuild();
        recordingsList.ClearSelection(); // clear the selected element

        selectedFile = null;
        playButton.SetEnabled(false);
        selectedLabel.text = "No file selected";
        statusLabel.text = fileNames.Count == 0
            ? "No recordings found."
            : $"{fileNames.Count} recording(s) found.";
    }

    private void OnSelectionChanged(IEnumerable<object> selected)
    {
        foreach (var item in selected)
        {
            selectedFile = item as string;
            selectedLabel.text = $"Selected: {selectedFile}";
            playButton.SetEnabled(true);
            return;
        }
    }

    private void OnPlayPressed()
    {
        if (string.IsNullOrEmpty(selectedFile)) return;
        statusLabel.text = $"Starting playback: {selectedFile}";
        arPlayback.StartPlayback(selectedFile);
    }

    private void OnStopPressed()
    {
        statusLabel.text = "Playback stopped.";
        arPlayback.StopPlayback();
    }

    void TogglePanel()
    {
        isVisible = !isVisible;
        playbackPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        //playbackPanelRenderer.enabled = isVisible;
    }

    void ToggleUIRenderer()
    {
        isVisible = !isVisible;
        this.gameObject.SetActive(isVisible);
    }
}
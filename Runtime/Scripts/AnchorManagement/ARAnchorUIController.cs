using System;
using UnityEngine;
using UnityEngine.UIElements;

using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class ARAnchorUIController : MonoBehaviour
{
    [SerializeField] AnchorData anchorData;
    [SerializeField] ARAnchorOrganizer aRAnchorOrganizer;

    private UIDocument uiDocument;
    private VisualElement anchorPanel;
    private ListView anchorsList;
    private Button anchorClearButton;
    private Button anchorPanelButton;

    private List<string> anchorTargetNames = new List<string>();
    private bool visible = false;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("ARTargetsUIController - UI Document was null");
            return;
        }

        if (anchorData == null)
        {
            Debug.LogError("ARTargetsUIController - AnchorData was null");
            return;
        }
        
        //anchorData.AnchorsUpdated += RefreshList; // listen to whenever anchors are updated - automatically keep the list in sync with these changes

        BindUI(uiDocument.rootVisualElement);

        anchorData.AnchorsUpdated += UpdateAnchorList;

    }

    private void OnDisable()
    {
        anchorData.AnchorsUpdated -= UpdateAnchorList;
        //anchorData.AnchorsUpdated -= RefreshList;
    }

    private void BindUI(VisualElement root)
    {
        root.pickingMode = PickingMode.Ignore;

        var uiPanel = root.Q<VisualElement>("ar-session-ui");
        anchorPanel = root.Q<VisualElement>("anchor-panel");
        anchorsList = root.Q<ListView>("anchor-list");
        anchorClearButton = root.Q<Button>("anchor-clear-button");
        anchorPanelButton = root.Q<Button>("anchor-panel-button");

        uiPanel.pickingMode = PickingMode.Ignore;
        anchorPanel.pickingMode = PickingMode.Ignore;

        root.RegisterCallback<PointerDownEvent>(evt =>
        {
            Debug.Log($"Pointer target: {evt.target}");
        });


        Func<VisualElement> makeItem = () =>
        {
            var anchorInfoVisualElement = new AnchorInfoVisualElement();
            var anchorRemoveButton = anchorInfoVisualElement.Q<Button>(name: "anchor-remove-button");
            anchorRemoveButton.clicked += () =>
            {
                string key = (string)anchorRemoveButton.userData;
                RemoveAnchor(key);
            };

            return anchorInfoVisualElement;
        };

        Action<VisualElement, int> bindItem = (e, i) => BindItem(e as AnchorInfoVisualElement, i);

        anchorsList.makeItem = makeItem;
        anchorsList.bindItem = bindItem;
        anchorsList.itemsSource = anchorTargetNames;

        anchorPanelButton.pickingMode = PickingMode.Position;
        anchorPanelButton.clicked += () => {
            //Debug.Log("Toggle Anchor Panel!");
            visible = !visible;
            anchorPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        };

        anchorClearButton.pickingMode = PickingMode.Position;
        anchorClearButton.clicked += () =>
        {
            foreach (string target in anchorTargetNames)
            {
                aRAnchorOrganizer.RemoveAnchor(target);
            }
            RefreshList();
        };
    }

    private void BindItem(AnchorInfoVisualElement elem, int i)
    {
        string key = anchorTargetNames[i];
        elem.Q<Label>("anchor-name-label").text = key;

        var button = elem.Q<Button>("anchor-remove-button");
        button.pickingMode = PickingMode.Position;
        button.userData = key;
    }

    private void UpdateAnchorList()
    {
        Debug.Log("Updating the Anchor List");
        RefreshList(); //  pull the latest version of the stored anchor targets
    }

    private void PopulateAnchorList()
    {

    }

    private void RefreshList()
    {
        anchorTargetNames.Clear();
        anchorTargetNames.AddRange(anchorData.GetKeys());
        anchorsList.Rebuild(); // automatically rebuild the list whenever the underlying list of anchors changes
    }

    private void RemoveAnchor(string key)
    {
        anchorData.RemoveAnchor(key); // remove the anchor from the AnchorData SO and the AnchorMAnager AR component
        RefreshList(); // refresh the list of anchors and their names to sync with now deleted one.
    }

    public class AnchorInfoVisualElement : VisualElement
    {
        public AnchorInfoVisualElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.justifyContent = Justify.SpaceBetween;

            var anchorNameLabel = new Label() { name = "anchor-name-label" };
            anchorNameLabel.style.flexGrow = 1;

            var anchorRemoveButton = new Button() { name = "anchor-remove-button" };
            

            root.Add(anchorNameLabel);
            root.Add(anchorRemoveButton);
            Add(root);
        }
    }
}

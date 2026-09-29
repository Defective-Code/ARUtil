using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using static SceneListUI;

public class SceneListUI : UIViewBehaviour
{
    [SerializeField] List<string> sceneNames;

    private Button scenePanelButton;
    private VisualElement scenePanel;
    private ListView sceneList;

    private bool visible;

    protected override void OnInitialize()
    {
        BindUI(Root);

    }

    private void BindUI(VisualElement root)
    {
        root.pickingMode = PickingMode.Ignore;

        //var uiPanel = root.Q<VisualElement>("ar-session-ui");
        scenePanel = root.Q<VisualElement>("scene-panel");
        sceneList = root.Q<ListView>("scene-list");
        scenePanelButton = root.Q<Button>("scene-panel-button");

        //uiPanel.pickingMode = PickingMode.Ignore;
        scenePanel.pickingMode = PickingMode.Ignore;

        root.RegisterCallback<PointerDownEvent>(evt =>
        {
            Debug.Log($"Pointer target: {evt.target}");
        });


        Func<VisualElement> makeItem = () =>
        {
            var sceneVisualElement = new SceneVisualElement();
            var sceneLoadButton = sceneVisualElement.Q<Button>(name: "scene-load-button");
            sceneLoadButton.clicked += () =>
            {
                string key = (string)sceneLoadButton.userData;
                SceneSwitcher.Instance.SwitchScene(key); // switch to the scene specfied by the button
            };

            return sceneVisualElement;
        };

        Action<VisualElement, int> bindItem = (e, i) => BindItem(e as SceneVisualElement, i);

        sceneList.makeItem = makeItem;
        sceneList.bindItem = bindItem;
        sceneList.itemsSource = sceneNames;

        scenePanelButton.pickingMode = PickingMode.Position;
        scenePanelButton.clicked += () => {
            //Debug.Log("Toggle Anchor Panel!");
            visible = !visible;
            //anchorPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            UIManager.Instance.Pop();
        };
    }

    private void BindItem(SceneVisualElement elem, int i)
    {
        string key = sceneNames[i];
        elem.Q<Label>("scene-name-label").text = key;

        var button = elem.Q<Button>("scene-load-button");
        button.pickingMode = PickingMode.Position;
        button.userData = key;
    }


    public class SceneVisualElement : VisualElement
    {
        public SceneVisualElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.justifyContent = Justify.SpaceBetween;
            root.style.backgroundColor = Color.red;

            var sceneNameLabel = new Label() { name = "scene-name-label" };
            sceneNameLabel.style.flexGrow = 1;

            var sceneLoadButton = new Button() { name = "scene-load-button" };

            root.Add(sceneNameLabel);
            root.Add(sceneLoadButton);
            Add(root);
        }
    }
}

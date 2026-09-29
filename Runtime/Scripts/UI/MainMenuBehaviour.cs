using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuBehaviour : UIViewBehaviour
{
    [SerializeField]
    private string sceneToLoadFirst;

    protected override void OnInitialize()
    {
        Root.Q<Button>("start-button").clicked += () =>
        {
            UIManager.Instance.SetHudVisible(true); // set the hud to be visible 
            UIManager.Instance.Pop();
            SceneSwitcher.Instance.SwitchScene(sceneToLoadFirst);  // load the scene specified in the editor field
        };
        Root.Q<Button>("map-menu-button").clicked += () => UIManager.Instance.Push(UIScreen.Map);
    }
}

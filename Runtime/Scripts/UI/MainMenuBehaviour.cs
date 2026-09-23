using UnityEngine.UIElements;

public class MainMenuBehaviour : UIViewBehaviour
{
    protected override void OnInitialize()
    {
        Root.Q<Button>("start-button").clicked += () =>
        {
            UIManager.Instance.SetHudVisible(true); // set the hud to be visible 
            UIManager.Instance.Pop();
        };
        Root.Q<Button>("anchor-menu-button").clicked += () => UIManager.Instance.Push(UIScreen.AnchorMenu);
        Root.Q<Button>("playback-menu-button").clicked += () => UIManager.Instance.Push(UIScreen.PlaybackMenu);
        Root.Q<Button>("map-menu-button").clicked += () => UIManager.Instance.Push(UIScreen.Map);
    }
}

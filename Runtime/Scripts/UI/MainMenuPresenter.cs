using UnityEngine.UIElements;

public class MainMenuPresenter : UIViewPresenter
{
    protected override void OnInitialize()
    {
        Root.Q<Button>("anchor-button").clicked += () => UIManager.Instance.Push(UIScreen.AnchorMenu);
        Root.Q<Button>("playback-button").clicked += () => UIManager.Instance.Push(UIScreen.PlaybackMenu);
        Root.Q<Button>("map-button").clicked += () => UIManager.Instance.Push(UIScreen.Map);
    }
}

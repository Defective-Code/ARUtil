using UnityEngine;
using UnityEngine.UIElements;

public class SceneSwitchingUI : UIViewBehaviour
{
    private ProgressBar loadingBar;

    protected override void OnInitialize()
    {
        loadingBar = Root.Q<ProgressBar>("loading-bar");

    }

    public void UpdateSliderValue(float value)
    {
        loadingBar.value = value;
    }

    public void UpdateSliderText(string text)
    {
        loadingBar.title = text;
    }

}

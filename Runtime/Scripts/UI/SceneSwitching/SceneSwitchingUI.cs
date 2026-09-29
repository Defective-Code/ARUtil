using UnityEngine;
using UnityEngine.UIElements;

public class SceneSwitchingUI : UIViewBehaviour
{
    private Slider loadingBar;

    protected override void OnInitialize()
    {
        loadingBar = Root.Q<Slider>("loading-bar");

    }

    public void UpdateSliderValue(float value)
    {
        loadingBar.value = value;
    }

}

using UnityEngine;
using UnityEngine.UI;

public class LoadingCanvas : MonoBehaviour
{
    public static LoadingCanvas Instance;

    public GameObject loadingCanvas;
    public Slider loadingBar;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Show()
    {
        loadingCanvas.SetActive(true);
    }

    public void Hide()
    {
        loadingCanvas.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        loadingBar.value = progress;
    }
}

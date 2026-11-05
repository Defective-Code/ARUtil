using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// Class to switch a scene asynchronously, showing a loading bar when doing so
public class SceneSwitcher : MonoBehaviour
{
    //private Button sceneSwitchButton;
    //public string SceneToLoad;
    //public GameObject loadingPanel;
    //public Slider loadingBar;

    public static SceneSwitcher Instance { get; private set; }

    //[SerializeField] 
    //private SceneSwitchingUI ui;
    //private UIManager uiManager;

    [SerializeField] private AnchorData anchorData;

    private ArPlayback playbackComponent;

    // These two are related fields, as every site has an id and a location, with a corresponding scene name to go with it. 
    // Need to store both 
    //public int currentSiteId = -1;
    public string currentScene;

    private bool loading;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //sceneSwitchButton = GetComponent<Button>();

    }

    //public string[] GetScenes()
    //{

    //}

    public void SwitchScene(string scene)
    {

        Debug.Log($"Switching to {scene}");

        // reset any countdown timers in ObserverManager when we change scenes
        //if (ObserverManager.instance != null) ObserverManager.instance.OnSceneChange();

        //if (playbackComponent == null) playbackComponent = FindAnyObjectByType<ArPlayback>();

        //// if we are currently playing back a recording, we want to first stop the playback, then switch the scene.
        //if (playbackComponent != null && playbackComponent.playingBack)
        //{
        //    playbackComponent.StopPlayback();
        //}

        // Clean up the data from the previous scene when we load a new one.
        //ArRoot.Instance.ResetArSession();

        StartCoroutine(LoadScene(scene));
    }

    public void SwitchSceneAdditive(string scene)
    {
        Debug.Log($"Switching scene additively to {scene}");

        if (playbackComponent == null) playbackComponent = FindAnyObjectByType<ArPlayback>();

        // if we are currently playing back a recording, we want to first stop the playback, then switch the scene.
        if (playbackComponent != null && playbackComponent.playingBack)
        {
            playbackComponent.StopPlayback();
        }

        StartCoroutine(LoadScene(scene));
    }

    //void OnButtonClick()
    //{
    //    Debug.Log($"Switching to {SceneToLoad}");

    //    // reset any countdown timers in ObserverManager when we change scenes
    //    ObserverManager.instance.OnSceneChange();

    //    StartCoroutine(LoadScene());
    //}

    private IEnumerator LoadScene(string scene, bool additive = false)
    {
        if (loading) yield break; // guard against overlapping scene switches;
        loading = true;

        yield return null;

        UIManager uiManager = UIManager.Instance;
        uiManager.Push(UIScreen.SceneSwitcher);
        SceneSwitchingUI ui = (SceneSwitchingUI)uiManager.GetUIView(UIScreen.SceneSwitcher);
        ui.UpdateSliderText($"Switching scene to : {scene}");

        try
        {
            AsyncOperation loadingScene = additive ? SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive) : SceneManager.LoadSceneAsync(scene);

            if (loadingScene == null)
            {
                Debug.LogError($"Scene '{scene}' not found � is it added to Build Settings?");
                //lcanvas.Hide();
                uiManager.Pop();
                yield break;
            }

            while (!loadingScene.isDone)
            {
                //ui.UpdateSliderValue(Mathf.Clamp01(loadingScene.progress / 0.9f));
                ui.UpdateSliderValue(Mathf.Clamp01(loadingScene.progress / 0.9f) * 100f);
                yield return null;
            }
            currentScene = scene;
        }
        finally
        {
            uiManager.Pop(UIScreen.SceneSwitcher); // always runs once
            loading = false;
            ui.UpdateSliderValue(0.0f);
        }

        //AsyncOperation loadingScene = additive ? SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive) : SceneManager.LoadSceneAsync(scene);

        //if (loadingScene == null)
        //{
        //    Debug.LogError($"Scene '{scene}' not found � is it added to Build Settings?");
        //    //lcanvas.Hide();
        //    uiManager.Pop();
        //    yield break;
        //}

        //loadingScene.allowSceneActivation = false;

        //// loops while the scene is loading, and updates the loadingbar
        //while (!loadingScene.isDone)
        //{
        //    //Debug.Log("Looping isDone");

        //    ui.UpdateSliderValue(Mathf.Clamp01(loadingScene.progress / 0.9f));

        //    if (loadingScene.progress >= 0.9f)
        //    {
        //        // The scene is done loading so we can fill the 
        //        loadingScene.allowSceneActivation = true;

        //        currentScene = scene;
        //    }

        //    yield return null;


        //}


        //uiManager.Pop(); // hide the loading bar as we are now finished loading the scene
        //ui.UpdateSliderValue(0.0f); // reset the loading bar value
    }
}
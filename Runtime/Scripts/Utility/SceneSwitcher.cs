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

    private LoadingCanvas lcanvas;

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

    void Start()
    {
        lcanvas = LoadingCanvas.Instance;

        if (lcanvas == null)
        {
            Debug.LogError("Loading canvas was null : Was unable to get the static instance of LoadingCanvas");
        }
        //sceneSwitchButton.onClick.AddListener(OnButtonClick);
    }

    public void SwitchScene(string scene)
    {
        
        Debug.Log($"Switching to {scene}");

        // reset any countdown timers in ObserverManager when we change scenes
        //if (ObserverManager.instance != null) ObserverManager.instance.OnSceneChange();

        StartCoroutine(LoadScene(scene));
    }

    //void OnButtonClick()
    //{
    //    Debug.Log($"Switching to {SceneToLoad}");

    //    // reset any countdown timers in ObserverManager when we change scenes
    //    ObserverManager.instance.OnSceneChange();

    //    StartCoroutine(LoadScene());
    //}

    private IEnumerator LoadScene(string scene)
    {
        //Debug.Log("Loading the scene");

        yield return null;

        //loadingPanel.SetActive(true);
        lcanvas.Show();

        AsyncOperation loadingScene = SceneManager.LoadSceneAsync(scene);

        if (loadingScene == null)
        {
            Debug.LogError($"Scene '{scene}' not found — is it added to Build Settings?");
            lcanvas.Hide();
            yield break;
        }

        loadingScene.allowSceneActivation = false;

        // loops while the scene is loading, and updates the loadingbar
        while (!loadingScene.isDone)
        {
            //Debug.Log("Looping isDone");

            lcanvas.SetProgress(Mathf.Clamp01(loadingScene.progress / 0.9f));

            if (loadingScene.progress >= 0.9f)
            {
                // The scene is done loading so we can fill the 
                loadingScene.allowSceneActivation = true;
            }

            yield return null;

            
        }

        // Reinit Vuforia only if the new scene has a VuforiaBehaviour
        //if (VuforiaBehaviour.Instance != null)
        //{
        //    VuforiaApplication.Instance.Initialize(); // or Init() depending on your SDK version
        //}

        lcanvas.Hide(); // hide the loading bar as we are now finished loading the scene
        lcanvas.SetProgress(0.0f); // reset the loading bar value

        //yield return loadingScene; // wait for loading scene to conclude
        //if (ObserverManager.instance != null) ObserverManager.instance.isSceneChanging = false; // then set the isSceneChanging flag to false
    }
}
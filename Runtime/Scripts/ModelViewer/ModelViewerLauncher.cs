using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelViewerLauncher : MonoBehaviour
{
    [SerializeField] string viewerSceneName = "ModelViewer";
    [SerializeField] Camera arCamera;
    [SerializeField] Behaviour[] disableWhileViewing;  // AR touch/placement scripts, raycast managers' input, etc.

    bool isOpen;


    public void Open(GameObject prefab)
    {
        if (isOpen) return;
        isOpen = true;
        ModelViewerController.PendingPrefab = prefab;   // read by the controller on load
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        foreach (var b in disableWhileViewing) b.enabled = false;
        yield return SceneManager.LoadSceneAsync(viewerSceneName, LoadSceneMode.Additive);
    }

    public void Close()
    {
        if (!isOpen) return;
        StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        yield return SceneManager.UnloadSceneAsync(viewerSceneName);
        foreach (var b in disableWhileViewing) b.enabled = true;
        isOpen = false;
    }
}
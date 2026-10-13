using UnityEngine;
using UnityEngine.UI;

public class ModelViewerController : MonoBehaviour
{
    [Header("References")]
    public Transform modelRoot;
    public ModelViewerPivot pivot;

    [Header("Model")]
    public GameObject modelPrefab;

    [Header("UI")]
    public Button resetButton;
    public Toggle autoRotateToggle;

    public static GameObject PendingPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PendingPrefab != null) modelPrefab = PendingPrefab;

        LoadModel();

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(() => pivot.ResetView());
        }
        
        if (autoRotateToggle != null)
        {
            autoRotateToggle.isOn = pivot.autoRotate;
            autoRotateToggle.onValueChanged.AddListener(pivot.ToggleAutoRotation);
        }
    }

    void LoadModel()
    {
        if (modelPrefab == null || modelRoot == null) return;

        foreach (Transform child in modelRoot)
        {
            child.SetParent(null);          // so old renderers aren't counted in bounds
            Destroy(child.gameObject);
        }

        var instance = Instantiate(modelPrefab, modelRoot.position, modelRoot.rotation, modelRoot);
        foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("ModelViewer");

        CenterModel();

        // Put the pivot at the model's centre
        pivot.transform.position = modelRoot.position;
    }

    void CenterModel()
    {
        if (modelRoot.childCount == 0) return;

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // reposition children so pivot is centered
        Vector3 offset = modelRoot.position - bounds.center;
        foreach (Transform child in modelRoot)
        {
            child.position += offset;
        }
    }
}

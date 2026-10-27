using UnityEngine;

/// <summary>
/// Put this on a mesh object that stands in for a real-world object (building, hill, tree...).
/// It applies the depth-mask material (so the mesh is invisible but hides content behind it)
/// and draws a wireframe in the Scene view (and the Game view, if its Gizmos toggle is on)
/// so you can see and position it.
///
/// Create a material from the "AR/DepthMask" shader and assign it below.
/// Place proxies under the same AuthoringSpace as your content so they spawn and align with it.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OcclusionProxy : MonoBehaviour
{
    [Tooltip("A material using the AR/DepthMask shader.")]
    public Material depthMaskMaterial;

    public bool showWireframe = true;
    public Color wireframeColor = new Color(1f, 0f, 1f, 0.9f);

    void OnEnable()
    {
        Apply();
    }

    void Reset()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        var r = GetComponent<MeshRenderer>();
        if (r == null) return;

        if (depthMaskMaterial != null && r.sharedMaterial != depthMaskMaterial)
        {
            r.sharedMaterial = depthMaskMaterial;
        }

        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void OnDrawGizmos()
    {
        if (!showWireframe) return;

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Gizmos.color = wireframeColor;
        Gizmos.DrawWireMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
    }
}
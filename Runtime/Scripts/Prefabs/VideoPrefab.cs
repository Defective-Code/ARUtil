using UnityEngine;
using UnityEngine.Video;

public class VideoPrefab : MonoBehaviour
{
    public bool IsInitialized { get; private set; }

    public VideoPlayer videoPlayer;

    public float x_offset { get; set; }
    public float y_offset { get; set; }
    public float z_offset { get; set; }

    // The scale you set in the editor. Its Y (the video's height) is the size reference: the width is
    // derived from it using the video's aspect ratio, and Z is left alone.
    Vector3 _authoredScale;

    void Awake()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("No VideoPlayer assigned on VideoPrefab!", this);
            return;
        }

        // Remember the editor scale BEFORE anything changes it
        _authoredScale = videoPlayer.transform.localScale;

        videoPlayer.prepareCompleted += OnVideoPrepared;

        if (!videoPlayer.isPrepared)
            videoPlayer.Prepare();
        else
            OnVideoPrepared(videoPlayer);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        float width = vp.width;
        float height = vp.height;

        if (width <= 0f || height <= 0f)
        {
            Debug.LogWarning("Video dimensions not valid yet", this);
            return;
        }

        float aspect = width / height;

        // Keep the authored height, derive the width from the aspect ratio.
        // Always computed from the cached authored scale, so calling this twice gives the same result.
        vp.transform.localScale = new Vector3(
            _authoredScale.y * aspect,
            _authoredScale.y,
            _authoredScale.z);

        Debug.Log($"Video {width}x{height} (aspect {aspect:F3}) => scale {vp.transform.localScale}", this);

        IsInitialized = true;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    /// <summary>
    /// Editor helper: right-click the component header > "Apply Aspect Ratio (Editor)".
    /// Sets the quad's X scale from its current Y scale and the clip's aspect ratio, so the editor
    /// matches what you'll see at runtime. Change Y yourself first to resize the video.
    /// </summary>
    [ContextMenu("Apply Aspect Ratio (Editor)")]
    void ApplyAspectInEditor()
    {
        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Debug.LogWarning("Assign a VideoPlayer with a clip first.", this);
            return;
        }

        float aspect = (float)videoPlayer.clip.width / videoPlayer.clip.height;
        Transform t = videoPlayer.transform;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(t, "Apply video aspect ratio");
#endif
        Vector3 s = t.localScale;
        t.localScale = new Vector3(s.y * aspect, s.y, s.z);
    }

    /// <summary>
    /// Places this object relative to its parent. Offsets are in the parent's local space, so they follow the
    /// anchor. (Adding them to transform.position would use world axes, which in AR depend on the direction the
    /// device was facing when the session started.)
    /// </summary>
    public void Transform()
    {
        transform.localPosition = new Vector3(x_offset, y_offset, z_offset);
        transform.localRotation = Quaternion.identity;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}
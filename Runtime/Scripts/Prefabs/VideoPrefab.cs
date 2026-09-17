using UnityEngine;
using UnityEngine.Video;

//[RequireComponent(typeof(VideoPlayer))]
public class VideoPrefab : MonoBehaviour
{
    public bool IsInitialized {  get; private set; }

    public VideoPlayer videoPlayer;

    public float x_offset { get; set; }
    public float y_offset { get; set; }
    public float z_offset { get; set; }

    //public float aspect_ratio;

    void Awake()
    {
       //Get the VideoPlayer component on this prefab
       //videoPlayer = transform.GetChild(0).gameObject.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("No VideoPlayer found on this GameObject!");
            return;
        }

        // Subscribe to prepareCompleted event
        videoPlayer.prepareCompleted += OnVideoPrepared;

        // Prepare the video if not already
        if (!videoPlayer.isPrepared)
            videoPlayer.Prepare();
        else
            OnVideoPrepared(videoPlayer);

        //Transform();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        float width = vp.width;
        float height = vp.height;

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning("Video dimensions not valid yet");
            return;
        }

        float aspect = width / height;

        // Scale quad: keep height constant, adjust width
        videoPlayer.transform.localScale = new Vector3(
            1 * aspect,
            1,
            1f
        );

        Debug.Log($"Quad scaled to match video aspect ratio: {width}x{height} => scale {transform.localScale}");

        IsInitialized = true;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    public void Transform()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        transform.position = transform.position + new Vector3(x_offset, y_offset, z_offset);


    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}

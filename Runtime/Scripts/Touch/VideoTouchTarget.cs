using UnityEngine;
using UnityEngine.Video;

[RequireComponent (typeof(VideoPlayer))]
public class VideoTouchTarget : TouchObject
{
    VideoPlayer player;

    private void OnEnable()
    {
        player = GetComponent<VideoPlayer>();

        if (player != null)
        {
            OnTouch += ToggleVideo;
        }
        else
        {
            Debug.LogError("Please ensure that there is a VideoPlayer component on this GameObject as well");
            return;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            OnTouch -= ToggleVideo;
        }
    }

    private void ToggleVideo()
    {
        if (!player.isPlaying)
        {
            player.Play();
        }
        else
        {
            player.Pause();
        }
    }

}

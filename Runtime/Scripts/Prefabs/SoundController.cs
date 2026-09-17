using UnityEngine;

public class SoundController : MonoBehaviour
{
    public AudioSource player;

    //void Start()
    //{
    //    player = transform.GetChild(0).gameObject.GetComponent<AudioSource>();
    //}

    public void StartPlayer()
    {
        Debug.Log("Starting audio player");
        player.Play();
    }

    public void StopPlayer()
    {
        Debug.Log("Stopping audio player");
        player.Stop();
    }
}

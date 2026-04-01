using UnityEngine;

//More dynamic way of us doing the music for each scene
//We can just attach this script to an empty game object in each scene, and assign the music clip for that scene. Then it will automatically play the music when the scene starts.
//If its the same music as the previous scene, it will just continue playing without restarting. If it's different, it will fade out the previous music and fade in the new music.
public class SceneMusic : MonoBehaviour
{
    public AudioClip musicClip;

    void Start()
    {
        if (musicClip != null)
        {
            MusicManager.instance.PlayMusic(musicClip);
        }
    }
}

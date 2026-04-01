using UnityEngine;

//This will help with music management, such as fading in and out, and switching tracks. It will be a singleton so that it can be accessed from anywhere in the code.
//Also need to add a DONT DESTROY ON LOAD to this, so that it persists across some scenes - helps prevent same tracks from stopping and starting constantly
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    private AudioSource audioSource;

    //Master volume multiplier (0-1), saved in PlayerPrefs
    private float masterVolume = 1f;
    private const string VolumePrefKey = "MasterVolume";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing on MusicManager game object. Please add an AudioSource component.");
        }

        //Load saved volume preference
        masterVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
    }

    //Set the volume from a slider (0-1 range) and save the preference
    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
        PlayerPrefs.SetFloat(VolumePrefKey, masterVolume);
    }

    public float GetVolume()
    {
        return masterVolume;
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
    {
        if (audioSource.clip == clip && audioSource.isPlaying) return;
        if (audioSource.isPlaying)
        {
            StartCoroutine(FadeOutThenIn(clip, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeInMusic(clip, fadeDuration));
        }
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOutMusic(fadeDuration));
    }

    //Fading in and out the music
    private System.Collections.IEnumerator FadeInMusic(AudioClip clip, float duration)
    {
        audioSource.clip = clip;
        audioSource.Play();
        audioSource.volume = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            audioSource.volume = Mathf.Lerp(0f, masterVolume, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = masterVolume;
    }

    private System.Collections.IEnumerator FadeOutThenIn(AudioClip clip, float duration)
    {
        yield return StartCoroutine(FadeOutMusic(duration));
        yield return StartCoroutine(FadeInMusic(clip, duration));
    }

    private System.Collections.IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = audioSource.volume;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
    }
    
}

using UnityEngine;

//Manages sound effects for various game events, such as combat actions.
//This is separate from MusicManager to keep music and SFX distinct.
[RequireComponent(typeof(AudioSource))]
public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance { get; private set; }

    private AudioSource audioSource;

    //Sound clips for different effects
    [Header("Combat Sounds")]
    [SerializeField] private AudioClip physicalAttackSound;
    [SerializeField] private AudioClip magicalAttackSound;
    [SerializeField] private AudioClip healingSound;
    [SerializeField] private AudioClip deathSound;

    //SFX volume multiplier (0-1), saved in PlayerPrefs
    private float sfxVolume = 1f;
    private const string SFXVolumePrefKey = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing on SoundEffectManager game object. Please add an AudioSource component.");
        }

        //Load saved SFX volume preference
        sfxVolume = PlayerPrefs.GetFloat(SFXVolumePrefKey, 1f);
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume;
        }
    }

    //Set the SFX volume from a slider (0-1 range) and save the preference
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume;
        }
        PlayerPrefs.SetFloat(SFXVolumePrefKey, sfxVolume);
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    //Play a physical attack sound (e.g., sword swing, punch)
    public void PlayPhysicalAttackSound()
    {
        if (physicalAttackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(physicalAttackSound);
        }
    }

    //Play a magical attack sound (e.g., spell cast, zap)
    public void PlayMagicalAttackSound()
    {
        if (magicalAttackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(magicalAttackSound);
        }
    }

    //Play a healing sound (e.g., restorative magic)
    public void PlayHealingSound()
    {
        if (healingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healingSound);
        }
    }

    //Play a death sound (e.g., defeat groan)
    public void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }

    //Generic method to play any sound effect
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
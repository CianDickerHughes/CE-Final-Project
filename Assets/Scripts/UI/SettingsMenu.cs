using UnityEngine;
using UnityEngine.UI;

//Settings menu panel with a volume slider.
//Attach this to your Settings panel GameObject and assign the slider in the Inspector.
public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        //Initialise the slider to the current saved volume
        if (MusicManager.instance != null)
        {
            volumeSlider.value = MusicManager.instance.GetVolume();
        }

        //Listen for slider changes
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        if (MusicManager.instance != null)
        {
            MusicManager.instance.SetVolume(value);
        }
    }

    private void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}

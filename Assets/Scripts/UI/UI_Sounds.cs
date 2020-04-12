using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UI_Sounds : MonoBehaviour
{
    // Start is called before the first frame update
    AudioSource mainAudioSource;
    [SerializeField] AudioClip ButtonHover;
    [SerializeField] AudioClip ButtonClick;

    void Awake()
    {
        mainAudioSource = GetComponent<AudioSource>();
    }

    public void PlayButtonHover()
    {
        mainAudioSource.PlayOneShot(ButtonHover, SoundSettings.UIVolume);
    }

    public void PlayButtonClick()
    {
        mainAudioSource.PlayOneShot(ButtonClick, SoundSettings.UIVolume);
    }
}

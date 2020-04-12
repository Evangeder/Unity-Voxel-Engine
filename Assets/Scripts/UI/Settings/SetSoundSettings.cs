using UnityEngine;
using UnityEngine.UI;

public class SetSoundSettings : MonoBehaviour
{
    public void SetMasterVolume(Slider slider)
    {
        SoundSettings.Master = slider.value;
        MusicPlayer.ChangeMusicVolume();
    }

    public void SetMusicVolume(Slider slider)
    {
        SoundSettings.Music = slider.value;
        MusicPlayer.ChangeMusicVolume();
    }

    public void SetEnvironmentVolume(Slider slider)
    {
        SoundSettings.Environment = slider.value;
    }

    public void SetEffectsVolume(Slider slider)
    {
        SoundSettings.Effects = slider.value;
    }

    public void SetUIVolume(Slider slider)
    {
        SoundSettings.UI = slider.value;
    }
}
